using FluentValidation;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación del cambio manual de prioridad (HU-029 CA-09).
/// Solo se permiten los 4 niveles de OrdenPrioridad.
/// </summary>
public class PrioridadOrdenValidator : AbstractValidator<PrioridadRequestDto>
{
    private static readonly string[] PrioridadesValidas =
        { OrdenPrioridad.Critico, OrdenPrioridad.Alto, OrdenPrioridad.Normal, OrdenPrioridad.Bajo };

    public PrioridadOrdenValidator()
    {
        RuleFor(x => x.Prioridad)
            .NotEmpty().WithMessage("La prioridad es obligatoria")
            .Must(p => PrioridadesValidas.Contains(p))
            .WithMessage("Prioridad inválida. Valores válidos: CRITICO, ALTO, NORMAL, BAJO");

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("El motivo no puede superar 500 caracteres");
    }
}