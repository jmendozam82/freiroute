using FluentValidation;
using Freiroute.DTO.Empresa;

namespace Freiroute.BLL.Services;

// HU-001
public class EmpresaValidator : AbstractValidator<EmpresaRequestDto>
{
    public EmpresaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(3).WithMessage("Debe tener al menos 3 caracteres");

        RuleFor(x => x.Plan)
            .Must(p => p is "starter" or "professional" or "enterprise")
            .WithMessage("El plan debe ser starter, professional o enterprise");
    }
}
