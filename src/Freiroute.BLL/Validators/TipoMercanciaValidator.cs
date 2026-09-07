using FluentValidation;
using Freiroute.DTO.Mercancia;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de tipos de mercancía (HU-017). Incluye reglas HAZMAT:
/// clase ONU (1-9 con subclases), coherencia de flags y rango de
/// temperatura completo si requiere refrigeración (CA-02/CA-04).
/// </summary>
public class TipoMercanciaValidator : AbstractValidator<TipoMercanciaRequestDto>
{
    public TipoMercanciaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la mercancía es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Codigo)
            .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Codigo));

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.Categoria)
            .MaximumLength(100).WithMessage("La categoría no puede exceder 100 caracteres");

        // ── HAZMAT (CA-02) ───────────────────────────────────────
        RuleFor(x => x.ClasePeligrosidad)
            .Matches("^[1-9](\\.[0-9])?$")
            .WithMessage("La clase de peligrosidad debe ser 1-9 con subclase opcional (ej: 3, 2.1)")
            .When(x => !string.IsNullOrWhiteSpace(x.ClasePeligrosidad));

        RuleFor(x => x.CodigoOnu)
            .MaximumLength(10).WithMessage("El código ONU no puede exceder 10 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.CodigoOnu));

        RuleFor(x => x.CodigoHs)
            .MaximumLength(20).WithMessage("El código HS no puede exceder 20 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.CodigoHs));

        // Si tiene clase de peligrosidad debe marcarse como peligrosa.
        RuleFor(x => x.EsPeligroso)
            .Equal(true).WithMessage("Una mercancía con clase de peligrosidad debe marcarse como peligrosa")
            .When(x => !string.IsNullOrWhiteSpace(x.ClasePeligrosidad));

        // ── Características físicas ──────────────────────────────
        RuleFor(x => x.PesoMaximoKg)
            .GreaterThan(0).WithMessage("El peso máximo debe ser mayor que cero")
            .When(x => x.PesoMaximoKg.HasValue);

        RuleFor(x => x.VolumenMaximoM3)
            .GreaterThan(0).WithMessage("El volumen máximo debe ser mayor que cero")
            .When(x => x.VolumenMaximoM3.HasValue);

        RuleFor(x => x.TemperaturaMinC)
            .LessThan(x => x.TemperaturaMaxC ?? decimal.MaxValue)
            .WithMessage("La temperatura mínima debe ser menor que la máxima")
            .When(x => x.TemperaturaMinC.HasValue && x.TemperaturaMaxC.HasValue);

        // CA-04: si requiere refrigeración, el rango de temperatura es completo.
        RuleFor(x => x.TemperaturaMinC)
            .NotNull().WithMessage("Debe indicar la temperatura mínima (°C) si requiere refrigeración")
            .When(x => x.RequiereRefrigeracion);

        RuleFor(x => x.TemperaturaMaxC)
            .NotNull().WithMessage("Debe indicar la temperatura máxima (°C) si requiere refrigeración")
            .When(x => x.RequiereRefrigeracion);
    }
}