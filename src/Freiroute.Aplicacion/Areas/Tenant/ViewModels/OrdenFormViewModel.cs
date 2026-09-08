using System.ComponentModel.DataAnnotations;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Freiroute.Aplicacion.Areas.Tenant.ViewModels;

/// <summary>
/// ViewModel del formulario de creación/edición de una orden de transporte
/// (HU-021). Los selects son Guid? con [Required] para que jQuery Validate
/// y el ModelState rechacen la opción vacía (un Guid no nulo no dispara
/// RequiredAttribute). Mensajes ES alineados con OrdenValidator.
/// </summary>
public class OrdenFormViewModel
{
    // ── Relaciones con maestros ───────────────────────────────────
    [Required(ErrorMessage = "El cliente es obligatorio")]
    public Guid? ClienteId { get; set; }

    [Required(ErrorMessage = "El origen es obligatorio")]
    public Guid? OrigenId { get; set; }

    [Required(ErrorMessage = "El destino es obligatorio")]
    public Guid? DestinoId { get; set; }

    [Required(ErrorMessage = "El tipo de mercancía es obligatorio")]
    public Guid? TipoMercanciaId { get; set; }

    [Required(ErrorMessage = "La unidad de medida es obligatoria")]
    public Guid? UnidadMedidaId { get; set; }

    public Guid? TipoEmbalajeId { get; set; }

    public Guid? TarifaId { get; set; }

    // ── Datos de carga ────────────────────────────────────────────
    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
    public decimal Cantidad { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El peso debe ser mayor a cero")]
    public decimal PesoKg { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El volumen debe ser mayor a cero")]
    public decimal? VolumenM3 { get; set; }

    public decimal? ValorDeclarado { get; set; }

    // ── Datos de servicio ─────────────────────────────────────────
    // Inicializadores con nombre de tipo completo: la propiedad
    // ModoTransporte/NivelServicio oculta la clase homónima para el
    // operador "." (clásico conflicto "Color Color" de C#).
    public string ModoTransporte { get; set; } = Freiroute.Utility.Constants.ModoTransporte.Terrestre;

    public string NivelServicio { get; set; } = Freiroute.Utility.Constants.NivelServicio.Estandar;

    public string Prioridad { get; set; } = OrdenPrioridad.Normal;

    // ── Fechas operativas ─────────────────────────────────────────
    public DateOnly? FechaPickupSolicitada { get; set; }

    public DateOnly? FechaEntregaRequerida { get; set; }

    // ── Referencia y comunicación ─────────────────────────────────
    [StringLength(100, ErrorMessage = "La referencia del cliente no puede exceder 100 caracteres")]
    public string? ReferenciaCliente { get; set; }

    [StringLength(1000, ErrorMessage = "Las instrucciones no pueden exceder 1000 caracteres")]
    public string? Instrucciones { get; set; }

    // ── Catálogos para selects ────────────────────────────────────
    public List<SelectListItem> Clientes { get; set; } = [];

    public List<SelectListItem> Ubicaciones { get; set; } = [];

    public List<SelectListItem> TiposMercancia { get; set; } = [];

    public List<SelectListItem> Unidades { get; set; } = [];

    public List<SelectListItem> Embalajes { get; set; } = [];

    public List<SelectListItem> Tarifas { get; set; } = [];

    /// <summary>Mapea el formulario al DTO de la API (empresa_id via JWT, ADR-003).</summary>
    public OrdenRequestDto ToRequest() => new()
    {
        ClienteId = ClienteId ?? Guid.Empty,
        OrigenId = OrigenId ?? Guid.Empty,
        DestinoId = DestinoId ?? Guid.Empty,
        TipoMercanciaId = TipoMercanciaId ?? Guid.Empty,
        UnidadMedidaId = UnidadMedidaId ?? Guid.Empty,
        TipoEmbalajeId = TipoEmbalajeId,
        TarifaId = TarifaId,
        Cantidad = Cantidad,
        PesoKg = PesoKg,
        VolumenM3 = VolumenM3,
        ValorDeclarado = ValorDeclarado,
        ModoTransporte = ModoTransporte,
        NivelServicio = NivelServicio,
        Prioridad = Prioridad,
        FechaPickupSolicitada = FechaPickupSolicitada,
        FechaEntregaRequerida = FechaEntregaRequerida,
        ReferenciaCliente = ReferenciaCliente,
        Instrucciones = Instrucciones,
    };
}