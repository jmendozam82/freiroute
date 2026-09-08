using FluentValidation;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

public class CambiarEstadoOrdenValidator : AbstractValidator<CambiarEstadoOrdenRequestDto>
{
    // Estados reconocidos por el sistema
    private static readonly HashSet<string> EstadosValidos =
        OrdenEstado.Labels.Keys.ToHashSet(StringComparer.Ordinal);

    public CambiarEstadoOrdenValidator()
    {
        RuleFor(x => x.EstadoNuevo)
            .NotEmpty()
            .WithMessage("El estado es obligatorio")
            .Must(e => EstadosValidos.Contains(e!))
            .WithMessage(x =>
                $"Estado '{x.EstadoNuevo}' no reconocido. " +
                $"Valores válidos: {string.Join(", ", EstadosValidos)}");

        RuleFor(x => x.Motivo)
            .MaximumLength(500)
            .WithMessage("El motivo no puede exceder 500 caracteres")
            .When(x => x.Motivo != null);
    }
}
