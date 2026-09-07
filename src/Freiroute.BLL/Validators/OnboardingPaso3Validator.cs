using FluentValidation;
using Freiroute.DTO.Onboarding;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación del Paso 3 del onboarding: configuración operativa (HU-012 CA-04).
/// Se valida el formato de colores, los prefijos y que los modos de transporte
/// sean códigos de <see cref="ModoTransporte"/> (fuente única en la capa Utility).
/// </summary>
public class OnboardingPaso3Validator : AbstractValidator<OnboardingPaso3RequestDto>
{
    public OnboardingPaso3Validator()
    {
        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria")
            .Length(3).WithMessage("La moneda debe ser el código ISO 4217 de 3 letras");

        RuleFor(x => x.ZonaHoraria)
            .NotEmpty().WithMessage("La zona horaria es obligatoria")
            .MaximumLength(100).WithMessage("La zona horaria no puede exceder 100 caracteres");

        RuleFor(x => x.FormatoFecha)
            .Must(f => new[] {
                "DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD"
            }.Contains(f))
            .WithMessage("Formato de fecha inválido. " +
                "Valores permitidos: DD/MM/YYYY, MM/DD/YYYY, YYYY-MM-DD")
            .When(x => !string.IsNullOrWhiteSpace(x.FormatoFecha));

        RuleFor(x => x.PrefijoEmbarque)
            .NotEmpty().WithMessage("El prefijo de embarque es obligatorio")
            .MaximumLength(10).WithMessage("El prefijo de embarque no puede exceder 10 caracteres")
            .Matches("^[A-Z0-9]+$").WithMessage("El prefijo solo admite letras mayúsculas y números");

        RuleFor(x => x.PrefijoOrden)
            .NotEmpty().WithMessage("El prefijo de orden es obligatorio")
            .MaximumLength(10).WithMessage("El prefijo de orden no puede exceder 10 caracteres")
            .Matches("^[A-Z0-9]+$").WithMessage("El prefijo solo admite letras mayúsculas y números");

        RuleFor(x => x.ModosTransporteActivos)
            .NotEmpty()
            .WithMessage("Debe seleccionar al menos un modo de transporte");

        // Nivel colección: conserva PropertyName = "ModosTransporteActivos"
        // (compatibilidad con mensajes y tests de Sprint 2).
        RuleFor(x => x.ModosTransporteActivos)
            .Must(modos => modos.All(m => ModoTransporte.Todos.Contains(m)))
            .WithMessage("Un modo de transporte no es válido")
            .When(x => x.ModosTransporteActivos is { Count: > 0 });

        // Per-element (Fase 3): mensaje con el modo específico.
        RuleForEach(x => x.ModosTransporteActivos)
            .Must(modo => ModoTransporte.Todos.Contains(modo))
            .WithMessage("El modo de transporte '{PropertyValue}' no es válido");
    }
}
