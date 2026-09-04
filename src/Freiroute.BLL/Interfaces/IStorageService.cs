namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de almacenamiento de archivos en Supabase Storage (HU-014, ADR-012).
/// Los buckets son PRIVADOS — las URLs se generan on-demand como signed URLs
/// temporales (AGENTS.md regla #32). Los archivos se suben con la
/// SERVICE_ROLE_KEY via REST (POST /storage/v1/object/{bucket}/{path}).
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Sube un archivo a un bucket y devuelve el path almacenado.
    /// El nombre se compone con un UUID para evitar colisiones.
    /// </summary>
    /// <param name="bucket">Nombre del bucket (ej: 'logos-tenants').</param>
    /// <param name="path">Carpeta destino (ej: '{empresa_id}').</param>
    /// <param name="fileName">Nombre original del archivo (se usa la extensión).</param>
    /// <param name="stream">Contenido del archivo.</param>
    /// <param name="contentType">Tipo MIME del archivo.</param>
    /// <returns>Path almacenado (ej: '66a6.../logo.png').</returns>
    Task<string> UploadAsync(string bucket, string path, string fileName,
        Stream stream, string contentType);

    /// <summary>
    /// Genera una signed URL temporal para descargar un objeto privado.
    /// </summary>
    /// <param name="bucket">Nombre del bucket.</param>
    /// <param name="path">Path del objeto.</param>
    /// <param name="expiresInSeconds">Vigencia de la URL en segundos (default 86400 = 24 h).</param>
    Task<string?> GetSignedUrlAsync(string bucket, string path, int expiresInSeconds = 86400);

    /// <summary>Elimina un objeto del bucket.</summary>
    Task<bool> DeleteAsync(string bucket, string path);
}
