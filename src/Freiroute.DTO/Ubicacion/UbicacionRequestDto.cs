using Freiroute.Utility.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace Freiroute.DTO.Ubicacion;

/// <summary>
/// Datos de entrada para crear o actualizar una ubicación (HU-015).
/// Al guardar con dirección, la BLL geocodifica automáticamente (ADR-014).
/// </summary>
[SwaggerSchema(Description = "Datos para crear o actualizar una ubicación")]
public class UbicacionRequestDto
{
    [SwaggerSchema(Description = "Nombre de la ubicación", Nullable = false)]
    public string Nombre { get; set; } = string.Empty;

    [SwaggerSchema(Description = "Código corto único por empresa (ej: BOD-MGA-01)")]
    public string? Codigo { get; set; }

    [SwaggerSchema(Description = "Tipo: ALMACEN, CLIENTE, PUERTO, AEROPUERTO, TERMINAL, CRUCE_FRONTERA, PUNTO_RECARGA, OTRO")]
    public string Tipo { get; set; } = TipoUbicacion.Otro;

    [SwaggerSchema(Description = "Dirección textual (se geocodifica automáticamente)")]
    public string? Direccion { get; set; }

    [SwaggerSchema(Description = "País")]
    public string Pais { get; set; } = "Nicaragua";

    [SwaggerSchema(Description = "Departamento / estado / provincia")]
    public string? Departamento { get; set; }

    [SwaggerSchema(Description = "Ciudad")]
    public string? Ciudad { get; set; }

    [SwaggerSchema(Description = "Código postal")]
    public string? CodigoPostal { get; set; }

    [SwaggerSchema(Description = "Nombre de la persona de contacto en sitio")]
    public string? ContactoNombre { get; set; }

    [SwaggerSchema(Description = "Teléfono de contacto en sitio")]
    public string? ContactoTelefono { get; set; }

    [SwaggerSchema(Description = "Email de contacto en sitio")]
    public string? ContactoEmail { get; set; }

    [SwaggerSchema(Description = "Hora de apertura en formato HH:mm (24h)")]
    public string? HorarioApertura { get; set; }

    [SwaggerSchema(Description = "Hora de cierre en formato HH:mm (24h)")]
    public string? HorarioCierre { get; set; }

    [SwaggerSchema(Description = "Tiempo estimado de carga/descarga en minutos (default 30)")]
    public int TiempoServicioMin { get; set; } = 30;

    [SwaggerSchema(Description = "Instrucciones de acceso o manejo")]
    public string? Instrucciones { get; set; }

    // ── Coordenadas manuales opcionales ───────────────────────
    [SwaggerSchema(Description = "Latitud manual (-90 a 90). Si se omite, se geocodifica")]
    public double? Latitud { get; set; }

    [SwaggerSchema(Description = "Longitud manual (-180 a 180). Si se omite, se geocodifica")]
    public double? Longitud { get; set; }
}