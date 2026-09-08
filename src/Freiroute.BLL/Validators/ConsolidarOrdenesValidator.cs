using FluentValidation;
using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Validators;

public class ConsolidarOrdenesValidator : AbstractValidator<ConsolidarOrdenesRequestDto>
{
    public ConsolidarOrdenesValidator()
    {
        RuleFor(x => x.OrdenIds)
            .NotEmpty().WithMessage("Debe seleccionar al menos 2 órdenes")
            .Must(ids => ids != null && ids.Count >= 2)
            .WithMessage("Se requieren mínimo 2 órdenes para consolidar");  // CA-02 HU-025

        RuleFor(x => x.OrdenIds)
            .Must(ids => ids != null && ids.Distinct().Count() == ids.Count)
            .WithMessage("No puede consolidar la misma orden dos veces")
            .When(x => x.OrdenIds != null);
    }
}
