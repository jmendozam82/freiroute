using FluentValidation;
using Freiroute.DTO.Suscripcion;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de suscripciones nuevas (HU-011).
/// El ciclo de facturación debe ser MENSUAL o ANUAL (ADR-004) y el precio
/// pactado no puede ser negativo.
/// </summary>
public class SuscripcionValidator : AbstractValidator<SuscripcionRequestDto>
{
    private static readonly string[] CiclosValidos = [TipoCiclo.MENSUAL, TipoCiclo.ANUAL];

    public SuscripcionValidator()
    {
        RuleFor(x => x.EmpresaId)
            .NotEmpty().WithMessage("La empresa es obligatoria");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("El plan es obligatorio");

        RuleFor(x => x.TipoCiclo)
            .Must(ciclo => CiclosValidos.Contains(ciclo))
            .WithMessage("El ciclo debe ser MENSUAL o ANUAL");

        RuleFor(x => x.PrecioPactado)
            .GreaterThanOrEqualTo(0).WithMessage("El precio pactado no puede ser negativo");

        RuleFor(x => x.MonedaPactada)
            .NotEmpty().WithMessage("La moneda es obligatoria")
            .MaximumLength(10).WithMessage("La moneda no puede exceder 10 caracteres");
    }
}
