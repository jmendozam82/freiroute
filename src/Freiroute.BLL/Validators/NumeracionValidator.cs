using FluentValidation;
using Freiroute.DTO.Configuracion;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de los prefijos de numeración de documentos del tenant (HU-014 CA-05).
/// Los prefijos no pueden estar vacíos ni exceder 10 caracteres; no pueden contener
/// espacios ni caracteres especiales (para numeraciones tipo FR-000001).
/// </summary>
public class NumeracionValidator : AbstractValidator<NumeracionRequestDto>
{
    public NumeracionValidator()
    {
        RuleFor(x => x.PrefijoEmbarque)
            .NotEmpty().WithMessage("El prefijo de embarques es obligatorio")
            .MaximumLength(10).WithMessage("El prefijo de embarques no puede exceder 10 caracteres")
            .Matches("^[A-Za-z0-9_\\-]+$").WithMessage("El prefijo solo admite letras, números, guiones y guion bajo");

        RuleFor(x => x.PrefijoOrden)
            .NotEmpty().WithMessage("El prefijo de órdenes es obligatorio")
            .MaximumLength(10).WithMessage("El prefijo de órdenes no puede exceder 10 caracteres")
            .Matches("^[A-Za-z0-9_\\-]+$").WithMessage("El prefijo solo admite letras, números, guiones y guion bajo");

        RuleFor(x => x.PrefijoCartaPorte)
            .NotEmpty().WithMessage("El prefijo de cartas de porte es obligatorio")
            .MaximumLength(10).WithMessage("El prefijo de cartas de porte no puede exceder 10 caracteres")
            .Matches("^[A-Za-z0-9_\\-]+$").WithMessage("El prefijo solo admite letras, números, guiones y guion bajo");
    }
}
