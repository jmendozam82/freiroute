namespace Freiroute.Utility.Exceptions;

/// <summary>
/// Excepción de acceso denegado por lógica de negocio (no por falta de claim
/// JWT — eso lo maneja [RequirePermission] con ForbidResult). Se usa cuando un
/// usuario autenticado intenta una operación que su rol no permite a nivel de
/// dominio. El GlobalExceptionMiddleware la mapea a HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}