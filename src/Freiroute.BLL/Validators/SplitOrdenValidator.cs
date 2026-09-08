using FluentValidation;
using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Validators;

public class SplitOrdenValidator : AbstractValidator<SplitOrdenRequestDto>
{
    public SplitOrdenValidator()
    {
        RuleFor(x => x.Splits)
            .NotEmpty().WithMessage("Debe especificar al menos 2 divisiones")
            .Must(s => s != null && s.Count >= 2)
            .WithMessage("Se requieren mínimo 2 divisiones")  // CA-03 HU-026
            .Must(s => s != null && s.Count <= 10)
            .WithMessage("El máximo de divisiones es 10");   // CA-03 HU-026

        RuleForEach(x => x.Splits)
            .ChildRules(split =>
            {
                split.RuleFor(s => s.Cantidad)
                    .GreaterThan(0)
                    .WithMessage("La cantidad de cada división debe ser mayor a cero");

                split.RuleFor(s => s.PesoKg)
                    .GreaterThan(0)
                    .WithMessage("El peso de cada división debe ser mayor a cero");
            })
            .When(x => x.Splits != null);
    }
}
