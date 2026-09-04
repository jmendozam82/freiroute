using FluentValidation;
using Freiroute.DTO.Suscripcion;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de registro de pagos (HU-011).
/// El pago es INMUTABLE (ADR-004): una vez registrado no se edita ni elimina.
/// El método de pago debe ser uno de los valores de Constants.MetodoPago.
/// </summary>
public class PagoValidator : AbstractValidator<PagoRequestDto>
{
    private static readonly string[] MetodosValidos =
    [
        MetodoPago.MANUAL,
        MetodoPago.STRIPE,
        MetodoPago.PAYPAL,
        MetodoPago.TRANSFERENCIA,
        MetodoPago.EFECTIVO
    ];

    public PagoValidator()
    {
        RuleFor(x => x.SuscripcionId)
            .NotEmpty().WithMessage("La suscripción es obligatoria");

        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero");

        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria")
            .MaximumLength(10).WithMessage("La moneda no puede exceder 10 caracteres");

        RuleFor(x => x.MetodoPago)
            .Must(metodo => MetodosValidos.Contains(metodo))
            .WithMessage("El método de pago no es válido");

        RuleFor(x => x.Referencia)
            .MaximumLength(200).WithMessage("La referencia no puede exceder 200 caracteres");

        RuleFor(x => x.PeriodoDesde)
            .NotEmpty().WithMessage("El inicio del período es obligatorio");

        RuleFor(x => x.PeriodoHasta)
            .NotEmpty().WithMessage("El fin del período es obligatorio")
            .GreaterThan(x => x.PeriodoDesde)
            .WithMessage("El fin del período debe ser posterior al inicio");
    }
}
