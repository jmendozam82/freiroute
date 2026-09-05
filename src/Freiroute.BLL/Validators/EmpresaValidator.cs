using FluentValidation;
using Freiroute.DTO.Empresa;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de registro/actualización de tenant (HU-001).
/// PlanSuscripcion: STARTER | PROFESSIONAL | ENTERPRISE (spec Sprint 1).
/// </summary>
public class EmpresaValidator : AbstractValidator<EmpresaRequestDto>
{
    private static readonly string[] PlanesValidos = ["STARTER", "PROFESSIONAL", "ENTERPRISE"];

    public EmpresaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la empresa es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.EmailAdmin)
            .NotEmpty().WithMessage("El email del administrador es obligatorio")
            .EmailAddress().WithMessage("El email del administrador no tiene un formato válido")
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres");

        RuleFor(x => x.Pais)
            .NotEmpty().WithMessage("El país es obligatorio")
            .MaximumLength(100).WithMessage("El país no puede exceder 100 caracteres");

        RuleFor(x => x.PlanSuscripcion)
            .Must(plan => PlanesValidos.Contains(plan))
            .WithMessage("El plan de suscripción debe ser STARTER, PROFESSIONAL o ENTERPRISE")
            .When(x => !string.IsNullOrWhiteSpace(x.PlanSuscripcion));

        // PlanId es opcional — si se envía debe ser un UUID válido (no vacío).
        RuleFor(x => x.PlanId)
            .NotEqual(Guid.Empty)
            .When(x => x.PlanId.HasValue)
            .WithMessage("El PlanId debe ser un UUID válido");

        RuleFor(x => x.ColorPrimario)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("El color primario debe ser un hex válido (#RRGGBB)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorPrimario));

        RuleFor(x => x.ColorSecundario)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .WithMessage("El color secundario debe ser un hex válido (#RRGGBB)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorSecundario));

        RuleFor(x => x.MonedaPrincipal)
            .MaximumLength(10).WithMessage("La moneda no puede exceder 10 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.MonedaPrincipal));
    }
}