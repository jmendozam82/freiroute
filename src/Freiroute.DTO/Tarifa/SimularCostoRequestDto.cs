using Freiroute.Utility.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Tarifa;

/// <summary>
/// Parámetros para simular el costo de un flete (HU-020 CA-04, ADR-015).
/// La BLL busca la tarifa vigente para la combinación zona-modo-servicio
/// y calcula el costo aplicando recargos.
/// </summary>
[SwaggerSchema(Description = "Parámetros para simular el costo de un flete")]
public class SimularCostoRequestDto
{
    [SwaggerSchema(Description = "ID de la zona de origen")]
    public Guid? ZonaOrigenId { get; set; }

    [SwaggerSchema(Description = "ID de la zona de destino")]
    public Guid? ZonaDestinoId { get; set; }

    [SwaggerSchema(Description = "Modo de transporte: FTL, LTL, AEREO, MARITIMO, FERROVIARIO, INTERMODAL")]
    public string ModoTransporte { get; set; } = Freiroute.Utility.Constants.ModoTransporte.Ftl;

    [SwaggerSchema(Description = "Tipo de servicio: ESTANDAR, EXPRESS, PROGRAMADO, REFRIGERADO, PELIGROSO")]
    public string TipoServicio { get; set; } = TipoServicioTransporte.Estandar;

    [SwaggerSchema(Description = "Peso de la carga en kg (aplica para POR_KG)")]
    public decimal PesoKg { get; set; }

    [SwaggerSchema(Description = "Volumen de la carga en m³ (aplica para POR_M3)")]
    public decimal VolumenM3 { get; set; }

    [SwaggerSchema(Description = "Distancia estimada en km (aplica para POR_KM)")]
    public decimal DistanciaKm { get; set; }

    [SwaggerSchema(Description = "Valor declarado de la carga (aplica para recargo SEGURO)")]
    public decimal ValorDeclarado { get; set; }

    [SwaggerSchema(Description = "Fecha de pickup planificada — determina la tarifa vigente")]
    public DateOnly FechaPickup { get; set; }
}