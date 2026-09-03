namespace Freiroute.Utility.Exceptions;

/// <summary>
/// Excepción de conflicto de estado/unicidad (ej: HU-001 CA-06 — email de
/// empresa duplicado). El GlobalExceptionMiddleware la mapea a HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}