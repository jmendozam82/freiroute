using System.Text.Json;
using FluentValidation;
using Freiroute.DTO.Zona;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de zonas de entrega (HU-016). Valida identificación,
/// color HEX, método de definición y que el polígono GeoJSON sea
/// un Polygon/MultiPolygon válido para ADR-018.
/// </summary>
public class ZonaEntregaValidator : AbstractValidator<ZonaRequestDto>
{
    private static readonly string[] TiposDefinicion =
    {
        TipoZonaDefinicion.Poligono, TipoZonaDefinicion.CodigosPostales,
        TipoZonaDefinicion.Ciudades, TipoZonaDefinicion.Departamentos,
        TipoZonaDefinicion.Paises
    };

    public ZonaEntregaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la zona es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código de la zona es obligatorio")
            .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("El código solo admite letras, números, guiones y guiones bajos");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.ColorHex)
            .NotEmpty().WithMessage("El color de la zona es obligatorio")
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color debe ser un hex válido (#RRGGBB)");

        RuleFor(x => x.TipoDefinicion)
            .NotEmpty().WithMessage("El método de definición es obligatorio")
            .Must(t => TiposDefinicion.Contains(t))
            .WithMessage("El método de definición no es válido");

        // ── Polígono (ADR-018): requerido y GeoJSON válido ───────
        RuleFor(x => x.PoligonoGeoJson)
            .NotEmpty().WithMessage("Debe indicar el polígono GeoJSON para zonas tipo POLIGONO")
            .MaximumLength(20000).WithMessage("El polígono no puede exceder 20000 caracteres")
            .Must(EsPoligonoGeoJsonValido).WithMessage("El polígono debe ser un GeoJSON Polygon o MultiPolygon válido")
            .When(x => x.TipoDefinicion == TipoZonaDefinicion.Poligono);

        // ── Listas ───────────────────────────────────────────────
        RuleFor(x => x.CodigosPostales)
            .NotEmpty().WithMessage("Debe indicar al menos un código postal para esta definición")
            .When(x => x.TipoDefinicion == TipoZonaDefinicion.CodigosPostales);

        RuleFor(x => x.Ciudades)
            .NotEmpty().WithMessage("Debe indicar al menos una ciudad para esta definición")
            .When(x => x.TipoDefinicion == TipoZonaDefinicion.Ciudades);

        RuleFor(x => x.Departamentos)
            .NotEmpty().WithMessage("Debe indicar al menos un departamento para esta definición")
            .When(x => x.TipoDefinicion == TipoZonaDefinicion.Departamentos);

        RuleFor(x => x.Paises)
            .NotEmpty().WithMessage("Debe indicar al menos un país para esta definición")
            .When(x => x.TipoDefinicion == TipoZonaDefinicion.Paises);

        RuleForEach(x => x.CodigosPostales)
            .MaximumLength(20).WithMessage("Un código postal no puede exceder 20 caracteres");

        RuleForEach(x => x.Ciudades)
            .MaximumLength(100).WithMessage("Una ciudad no puede exceder 100 caracteres");

        RuleForEach(x => x.Departamentos)
            .MaximumLength(100).WithMessage("Un departamento no puede exceder 100 caracteres");

        RuleForEach(x => x.Paises)
            .MaximumLength(100).WithMessage("Un país no puede exceder 100 caracteres");
    }

    /// <summary>
    /// Valida que el texto sea un GeoJSON con type Polygon o MultiPolygon
    /// y con al menos un anillo de coordenadas [longitud, latitud].
    /// </summary>
    private static bool EsPoligonoGeoJsonValido(string? geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var typeProp))
            {
                return false;
            }

            var type = typeProp.GetString();
            if (type is not ("Polygon" or "MultiPolygon"))
            {
                return false;
            }

            if (!root.TryGetProperty("coordinates", out var coords) || coords.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            if (type == "Polygon")
            {
                return TieneAnilloValido(coords);
            }

            // MultiPolygon: array de polígonos.
            foreach (var poligono in coords.EnumerateArray())
            {
                if (poligono.ValueKind == JsonValueKind.Array && !TieneAnilloValido(poligono))
                {
                    return false;
                }
            }

            return coords.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TieneAnilloValido(JsonElement polygon)
    {
        if (polygon.ValueKind != JsonValueKind.Array || polygon.GetArrayLength() == 0)
        {
            return false;
        }

        var anillo = polygon[0];
        if (anillo.ValueKind != JsonValueKind.Array || anillo.GetArrayLength() < 4)
        {
            return false; // Un polígono cerrado requiere al menos 4 puntos (el 1º = último).
        }

        foreach (var punto in anillo.EnumerateArray())
        {
            if (punto.ValueKind != JsonValueKind.Array || punto.GetArrayLength() < 2)
            {
                return false; // Cada punto es [longitud, latitud].
            }

            if (punto[0].TryGetDouble(out var lng) && (lng < -180 || lng > 180))
            {
                return false;
            }

            if (punto[1].TryGetDouble(out var lat) && (lat < -90 || lat > 90))
            {
                return false;
            }
        }

        return true;
    }
}