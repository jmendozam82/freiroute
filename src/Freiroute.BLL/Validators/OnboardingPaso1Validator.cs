using FluentValidation;
using Freiroute.DTO.Onboarding;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación del Paso 1 del onboarding: datos de la empresa (HU-012 CA-02).
/// </summary>
public class OnboardingPaso1Validator : AbstractValidator<OnboardingPaso1RequestDto>
{
    public OnboardingPaso1Validator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la empresa es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.RucNit)
            .MaximumLength(50).WithMessage("El RUC/NIT no puede exceder 50 caracteres");

        RuleFor(x => x.Direccion)
            .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres");

        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres");

        RuleFor(x => x.Industria)
            .MaximumLength(100).WithMessage("La industria no puede exceder 100 caracteres");
    }
}
