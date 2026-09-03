namespace Freiroute.Utility.Exceptions;

/// <summary>
/// Excepción de recurso no encontrado. La lanza la BLL cuando una entidad
/// buscada no existe (o no pertenece al tenant actual). El
/// GlobalExceptionMiddleware la mapea a HTTP 404 sin exponer detalles internos.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entidad, object id)
        : base($"{entidad} con id '{id}' no encontrado.")
    {
    }
}