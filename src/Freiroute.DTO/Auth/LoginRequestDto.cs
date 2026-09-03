namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada para iniciar sesión con email y contraseña.
/// La contraseña debe cumplir: mínimo 8 caracteres, 1 mayúscula, 1 número, 1 carácter especial.
/// </summary>
public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}