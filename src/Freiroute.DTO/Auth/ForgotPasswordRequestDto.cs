namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada para solicitar recuperación de contraseña.
/// Solo se solicita el email; la respuesta es genérica (no revela si el email existe).
/// </summary>
public class ForgotPasswordRequestDto
{
    public string Email { get; set; } = string.Empty;
}