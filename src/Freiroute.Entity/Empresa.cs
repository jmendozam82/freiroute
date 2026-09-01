using System.ComponentModel.DataAnnotations;

namespace Freiroute.Entity;

/// <summary>
/// Entidad que representa a una empresa (tenant) en el sistema SaaS Freiroute.
/// Corresponde a la tabla 'empresas' en Supabase/PostgreSQL.
/// </summary>
public class Empresa
{
    [Key]
    public Guid Id { get; set; }
    
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Plan { get; set; } = "starter"; // starter | professional | enterprise
    
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }
}
