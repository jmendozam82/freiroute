namespace Freiroute.Utility.Exceptions;

/// <summary>
/// Excepción especial lanzada durante el login cuando el usuario tiene 2FA activo
/// (HU-005). El GlobalExceptionMiddleware la intercepta y retorna HTTP 202 con el
/// Requires2faResponseDto (tempToken) para que el cliente llame a POST /api/auth/2fa/verify.
/// </summary>
public class Requires2faException : Exception
{
    /// <summary>Token temporal de corta vida (5 min) que el cliente debe enviar al paso 2.</summary>
    public string TempToken { get; }

    public Requires2faException(string tempToken)
        : base("2FA requerido")
        => TempToken = tempToken;
}
