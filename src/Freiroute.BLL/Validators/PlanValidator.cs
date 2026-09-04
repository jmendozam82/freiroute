using FluentValidation;
using Freiroute.DTO.Plan;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de planes de suscripción (HU-010).
/// El código debe ser único (STARTER, PROFESSIONAL, ENTERPRISE, o un código
/// personalizado para planes a medida). Los límites y precios son numéricos
/// y deben ser consistentes.
/// </summary>
public class PlanValidator : AbstractValidator<PlanRequestDto>
{
    public PlanValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del plan es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código del plan es obligatorio")
            .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres")
            .Matches("^[A-Z0-9_]+$").WithMessage("El código solo admite mayúsculas, números y guion bajo");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres");

        RuleFor(x => x.LimiteUsuarios)
            .Must(l => l == -1 || l > 0)
            .WithMessage("El límite de usuarios debe ser -1 (ilimitado) o un número positivo");

        RuleFor(x => x.LimiteEmbarquesMes)
            .Must(l => l == -1 || l > 0)
            .WithMessage("El límite de embarques debe ser -1 (ilimitado) o un número positivo");

        RuleFor(x => x.LimiteStorageGb)
            .GreaterThanOrEqualTo(0).WithMessage("El límite de almacenamiento no puede ser negativo");

        RuleFor(x => x.PrecioMensual)
            .GreaterThanOrEqualTo(0).WithMessage("El precio mensual no puede ser negativo");

        RuleFor(x => x.PrecioAnual)
            .GreaterThanOrEqualTo(0).WithMessage("El precio anual no puede ser negativo");

        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria")
            .MaximumLength(10).WithMessage("La moneda no puede exceder 10 caracteres");

        // HU-010 CA-02: los módulos disponibles deben seleccionarse de la lista de 12 módulos.
        RuleFor(x => x.ModulosDisponibles)
            .NotEmpty()
            .WithMessage("Debe especificar al menos un módulo disponible");

        RuleForEach(x => x.ModulosDisponibles)
            .Must(m => ModuloPermiso.Todos.Contains(m))
            .WithMessage(m =>
                $"'{m}' no es un módulo válido. " +
                $"Módulos válidos: {string.Join(", ", ModuloPermiso.Todos)}");
    }
}
