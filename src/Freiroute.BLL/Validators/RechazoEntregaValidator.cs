using FluentValidation;
using Freiroute.DTO.Orden;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación del registro de rechazo de entrega (HU-030 CA-01).
/// El motivo debe ser uno de los 5 valores del dominio de rechazos
/// (CLIENTE_AUSENTE · DIRECCION_INCORRECTA · MERCANCIA_DANADA ·
/// RECHAZO_CLIENTE · OTRO).
/// </summary>
public class RechazoEntregaValidator : AbstractValidator<RechazoEntregaRequestDto>
{
    private static readonly string[] MotivosValidos =
        { "CLIENTE_AUSENTE", "DIRECCION_INCORRECTA", "MERCANCIA_DANADA", "RECHAZO_CLIENTE", "OTRO" };

    public RechazoEntregaValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("El motivo del rechazo es obligatorio")
            .Must(m => MotivosValidos.Contains(m))
            .WithMessage("Motivo de rechazo inválido");

        RuleFor(x => x.Descripcion)
            .MaximumLength(2000).WithMessage("La descripción no puede superar 2000 caracteres");
    }
}