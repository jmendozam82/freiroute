using FluentValidation;
using Freiroute.DTO.Reclamo;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de creación de reclamos (HU-032 CA-01). El tipo debe ser
/// uno de: DANO · PERDIDA · RETRASO · OTRO. El monto es opcional y debe
/// ser positivo cuando se informa.
/// </summary>
public class ReclamoValidator : AbstractValidator<ReclamoRequestDto>
{
    private static readonly string[] TiposValidos =
        { "DANO", "PERDIDA", "RETRASO", "OTRO" };

    public ReclamoValidator()
    {
        RuleFor(x => x.OrdenId)
            .NotEmpty().WithMessage("La orden vinculada es obligatoria");

        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de reclamo es obligatorio")
            .Must(t => TiposValidos.Contains(t))
            .WithMessage("Tipo de reclamo inválido. Valores válidos: DANO, PERDIDA, RETRASO, OTRO");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción del reclamo es obligatoria")
            .MaximumLength(2000).WithMessage("La descripción no puede superar 2000 caracteres");

        RuleFor(x => x.MontoReclamado)
            .GreaterThan(0).WithMessage("El monto reclamado debe ser mayor a 0")
            .When(x => x.MontoReclamado.HasValue);
    }
}