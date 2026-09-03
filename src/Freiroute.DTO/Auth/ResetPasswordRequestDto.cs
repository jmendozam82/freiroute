namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada para restablecer la contraseña con el token recibido por email.
/// El token es de un solo uso y expira a los 30 minutos.
/// </summary>
public class ResetPasswordRequestDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}