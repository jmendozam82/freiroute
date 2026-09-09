using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Reclamo;

/// <summary>
/// Datos para la transición de estado de un reclamo (HU-032 CA-04).
/// El motivo es obligatorio y la transición debe ser válida según
/// EstadoReclamo.Transiciones (CA-03).
/// </summary>
[SwaggerSchema(Description = "Datos para cambiar el estado de un reclamo")]
public class ReclamoEstadoRequestDto
{
    [SwaggerSchema(Description = "Estado nuevo del reclamo: EN_REVISION | APROBADO | RECHAZADO | CERRADO")]
    public string EstadoNuevo { get; set; } = null!;

    [SwaggerSchema(Description = "Motivo del cambio (obligatorio)")]
    public string Motivo { get; set; } = null!;
}