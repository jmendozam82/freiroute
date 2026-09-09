using FluentValidation;
using Freiroute.DTO.Reclamo;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de transiciones de estado de reclamos (HU-032 CA-04/CA-12).
/// El motivo es obligatorio en toda transición (auditoría + historial).
/// La FSM la valida el servicio (EstadoReclamo.Transiciones).
/// </summary>
public class ReclamoEstadoValidator : AbstractValidator<ReclamoEstadoRequestDto>
{
    public ReclamoEstadoValidator()
    {
        RuleFor(x => x.EstadoNuevo)
            .NotEmpty().WithMessage("El estado nuevo es obligatorio");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo de la transición es obligatorio")
            .MaximumLength(1000).WithMessage("El motivo no puede superar 1000 caracteres");
    }
}