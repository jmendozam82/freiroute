namespace Freiroute.DTO.Empresa;

/// <summary>
/// Datos de salida de una empresa (tenant). Es la tabla raíz del SaaS,
/// por lo que no aplica ocultar EmpresaId — no lo posee.
/// </summary>
public class EmpresaResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? RucNit { get; set; }
    public string EmailAdmin { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Pais { get; set; } = string.Empty;
    public string? Ciudad { get; set; }
    public string? Direccion { get; set; }
    public string? LogoUrl { get; set; }
    public string ColorPrimario { get; set; } = string.Empty;
    public string ColorSecundario { get; set; } = string.Empty;
    public string PlanSuscripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string MonedaPrincipal { get; set; } = string.Empty;
    public string ZonaHoraria { get; set; } = string.Empty;
    public string Idioma { get; set; } = string.Empty;
    public string FormatoFecha { get; set; } = string.Empty;
    public string PrefijoEmbarque { get; set; } = string.Empty;
    public int ConsecutivoEmbarque { get; set; }
    public string PrefijoOrden { get; set; } = string.Empty;
    public int ConsecutivoOrden { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}