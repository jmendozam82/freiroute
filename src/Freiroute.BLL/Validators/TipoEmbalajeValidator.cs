using FluentValidation;
using Freiroute.DTO.Unidad;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de tipos de embalaje (HU-018). Valida identificación,
/// código corto (PLT, CAJA, TAM...) y capacidades no negativas.
/// </summary>
public class TipoEmbalajeValidator : AbstractValidator<TipoEmbalajeRequestDto>
{
    public TipoEmbalajeValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del embalaje es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código del embalaje es obligatorio")
            .MaximumLength(20).WithMessage("El código no puede exceder 20 caracteres")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("El código solo admite letras, números y guiones");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.CapacidadKg)
            .GreaterThan(0).WithMessage("La capacidad en kg debe ser mayor que cero")
            .When(x => x.CapacidadKg.HasValue);

        RuleFor(x => x.CapacidadM3)
            .GreaterThan(0).WithMessage("La capacidad en m³ debe ser mayor que cero")
            .When(x => x.CapacidadM3.HasValue);
    }
}