using Freiroute.DTO.Reclamo;
using Freiroute.Entity;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Tests.Builders;

/// <summary>
/// Builder para entidades Reclamo y sus DTOs (HU-032).
/// Patrón obligatorio para entidades con más de 5 campos requeridos (AGENTS.md §24).
/// </summary>
public class ReclamoBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _empresaId = Guid.NewGuid();
    private Guid _ordenId = Guid.NewGuid();
    private string? _numeroReclamo = "REC-2026-0001";
    private string _tipo = TipoReclamo.Dano;
    private string _descripcion = "Mercancía dañada durante el tránsito";
    private decimal? _montoReclamado = 1500.00m;
    private string _estado = EstadoReclamo.Abierto;
    private string[]? _referenciasEvidencia = { "https://storage.supabase.co/evidencia/pod.jpg" };
    private string? _ordenNumero = "ORD-2026-00001";
    private string? _clienteNombre = "Distribuidora ABC S.A.";

    public ReclamoBuilder ConId(Guid id) { _id = id; return this; }
    public ReclamoBuilder ConEmpresa(Guid id) { _empresaId = id; return this; }
    public ReclamoBuilder ConOrden(Guid id) { _ordenId = id; return this; }
    public ReclamoBuilder ConNumero(string? numero) { _numeroReclamo = numero; return this; }
    public ReclamoBuilder ConTipo(string tipo) { _tipo = tipo; return this; }
    public ReclamoBuilder ConDescripcion(string descripcion) { _descripcion = descripcion; return this; }
    public ReclamoBuilder ConMonto(decimal? monto) { _montoReclamado = monto; return this; }
    public ReclamoBuilder ConEstado(string estado) { _estado = estado; return this; }
    public ReclamoBuilder ConOrdenNumero(string? numero) { _ordenNumero = numero; return this; }
    public ReclamoBuilder ConClienteNombre(string? nombre) { _clienteNombre = nombre; return this; }

    public Reclamo Build() => new()
    {
        Id = _id,
        EmpresaId = _empresaId,
        OrdenId = _ordenId,
        NumeroReclamo = _numeroReclamo,
        Tipo = _tipo,
        Descripcion = _descripcion,
        MontoReclamado = _montoReclamado,
        Estado = _estado,
        ReferenciasEvidencia = _referenciasEvidencia,
        OrdenNumero = _ordenNumero,
        ClienteNombre = _clienteNombre,
        Activo = true,
        FechaCreacion = DateTime.UtcNow,
        FechaModificacion = DateTime.UtcNow
    };

    public ReclamoRequestDto BuildRequestDto() => new()
    {
        OrdenId = _ordenId,
        Tipo = _tipo,
        Descripcion = _descripcion,
        MontoReclamado = _montoReclamado,
        ReferenciasEvidencia = _referenciasEvidencia?.ToList()
    };

    public ReclamoEstadoRequestDto BuildEstadoRequestDto(string estadoNuevo, string motivo) => new()
    {
        EstadoNuevo = estadoNuevo,
        Motivo = motivo
    };
}