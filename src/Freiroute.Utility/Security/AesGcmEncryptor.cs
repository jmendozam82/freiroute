using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Freiroute.Utility.Security;

/// <summary>
/// Cifrado simétrico AES-256-GCM para material secreto en reposo (ADR-011).
/// Se usa para cifrar el secret TOTP antes de persistirlo en 'configuracion_2fa'.
///
/// Formato persistido: base64(iv[12 bytes] + tag[16 bytes] + ciphertext)
/// - IV aleatorio de 12 bytes por operación (nunca se reutiliza).
/// - Tag de autenticación GCM de 16 bytes (detección de manipulación).
/// - Clave maestra de 256 bits (32 bytes), derivada con SHA-256 si el string
///   de entorno tiene otra longitud.
///
/// La clave maestra proviene de IConfiguration["Security:TotpEncryptionKey"]
/// (variable de entorno TOTP_ENCRYPTION_KEY). Nunca se hardcodea (AGENTS.md #33).
/// </summary>
public static class AesGcmEncryptor
{
    private const int IvSizeBytes = 12;   // Tamaño recomendado del nonce para AES-GCM
    private const int TagSizeBytes = 16;  // Tag de autenticación estándar AES-GCM

    /// <summary>
    /// Cifra el texto plano con AES-256-GCM.
    /// Retorna: base64(iv[12] + tag[16] + ciphertext).
    /// </summary>
    /// <param name="plaintext">Texto a cifrar (ej: secret TOTP).</param>
    /// <param name="base64Key">Clave maestra en base64 (debe derivar en 32 bytes).</param>
    /// <returns>String en base64 con iv + tag + ciphertext concatenados.</returns>
    /// <exception cref="ArgumentNullException">Si plaintext o base64Key son nulos.</exception>
    /// <exception cref="CryptographicException">Si la clave master es inválida.</exception>
    public static string Encrypt(string plaintext, string base64Key)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext, nameof(plaintext));
        ArgumentException.ThrowIfNullOrEmpty(base64Key, nameof(base64Key));

        byte[] key = DeriveKey(base64Key);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);

        // IV aleatorio de 12 bytes por operación (nunca reutilizar)
        byte[] iv = RandomNumberGenerator.GetBytes(IvSizeBytes);
        byte[] ciphertext = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(iv, plainBytes, ciphertext, tag);

        // Concatenar iv + tag + ciphertext y codificar en base64
        byte[] result = new byte[iv.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(tag, 0, result, iv.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Descifra el string base64 producido por <see cref="Encrypt"/>.
    /// Si el tag de autenticación no coincide, lanza una excepción genérica.
    /// </summary>
    /// <param name="ciphertext">String en base64 (iv + tag + ciphertext).</param>
    /// <param name="base64Key">Clave maestra en base64 (debe derivar en 32 bytes).</param>
    /// <returns>El texto plano original.</returns>
    /// <exception cref="SecurityException">
    /// Si el descifrado falla (tag inválido, formato incorrecto o clave incorrecta).
    /// El mensaje es genérico para no revelar detalles del fallo.
    /// </exception>
    public static string Decrypt(string ciphertext, string base64Key)
    {
        ArgumentException.ThrowIfNullOrEmpty(ciphertext, nameof(ciphertext));
        ArgumentException.ThrowIfNullOrEmpty(base64Key, nameof(base64Key));

        try
        {
            byte[] key = DeriveKey(base64Key);
            byte[] data;

            try
            {
                data = Convert.FromBase64String(ciphertext);
            }
            catch (FormatException)
            {
                throw new SecurityException("Error de descifrado.");
            }

            // Verificar longitud mínima (iv + tag)
            if (data.Length < IvSizeBytes + TagSizeBytes)
            {
                throw new SecurityException("Error de descifrado.");
            }

            byte[] iv = new byte[IvSizeBytes];
            byte[] tag = new byte[TagSizeBytes];
            byte[] cipher = new byte[data.Length - IvSizeBytes - TagSizeBytes];

            Buffer.BlockCopy(data, 0, iv, 0, IvSizeBytes);
            Buffer.BlockCopy(data, IvSizeBytes, tag, 0, TagSizeBytes);
            Buffer.BlockCopy(data, IvSizeBytes + TagSizeBytes, cipher, 0, cipher.Length);

            byte[] plainBytes = new byte[cipher.Length];

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(iv, cipher, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (SecurityException)
        {
            throw;
        }
        catch (CryptographicException)
        {
            // Tag inválido o clave incorrecta — no revelar detalles
            throw new SecurityException("Error de descifrado.");
        }
    }

    /// <summary>
    /// Deriva una clave de 32 bytes (256 bits) a partir del string de configuración.
    /// Si el string ya tiene exactamente 32 bytes se usa directo; si no, se aplica
    /// SHA-256 para obtener una clave de largo fijo (ADR-011).
    /// </summary>
    private static byte[] DeriveKey(string base64Key)
    {
        // Intentar interpretar el valor como base64 de 32 bytes (formato de env recomendado)
        try
        {
            byte[] decoded = Convert.FromBase64String(base64Key);
            if (decoded.Length == 32)
            {
                return decoded;
            }
        }
        catch (FormatException)
        {
            // No era base64 — se trata como texto plano y se deriva con SHA-256
        }

        // Derivar con SHA-256 siempre produce 32 bytes válidos
        byte[] textBytes = Encoding.UTF8.GetBytes(base64Key);
        return SHA256.HashData(textBytes);
    }
}
