namespace Freiroute.Utility.Exceptions;

/// <summary>
/// Excepción de regla de negocio. La lanza la BLL cuando una operación
/// viola una regla del dominio TMS (validación de negocio, estado inválido,
/// conflicto de unicidad, etc.). El GlobalExceptionMiddleware la mapea a un
/// 4xx con mensaje claro para el usuario (sin exponer detalles internos).
/// </summary>
public class BusinessException : Exception
{
    /// <summary>Código de error estable del dominio (ej: "EMPESA_EMAIL_YA_EXISTE").</summary>
    public string Code { get; }

    public BusinessException(string message, string code = "BUSINESS_ERROR")
        : base(message)
    {
        Code = code;
    }

    public BusinessException(string message, Exception innerException, string code = "BUSINESS_ERROR")
        : base(message, innerException)
    {
        Code = code;
    }
}