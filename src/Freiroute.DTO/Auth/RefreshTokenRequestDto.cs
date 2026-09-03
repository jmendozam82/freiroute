namespace Freiroute.DTO.Auth;

/// <summary>
/// Datos de entrada para renovar el access token mediante el refresh token.
/// </summary>
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}