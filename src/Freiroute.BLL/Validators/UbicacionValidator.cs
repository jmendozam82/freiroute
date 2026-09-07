using FluentValidation;
using Freiroute.DTO.Ubicacion;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de ubicaciones (HU-015). Valida identificación, tipo,
/// dirección (se geocodifica automáticamente), horarios y coordenadas
/// manuales opcionales.
/// </summary>
public class UbicacionValidator : AbstractValidator<UbicacionRequestDto>
{
    public UbicacionValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la ubicación es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Codigo)
            .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("El código solo admite letras, números, guiones y guiones bajos")
            .When(x => !string.IsNullOrWhiteSpace(x.Codigo));

        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de ubicación es obligatorio")
            .Must(t => TipoUbicacion.Todos.Contains(t))
            .WithMessage("El tipo de ubicación no es válido");

        RuleFor(x => x.Direccion)
            .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres");

        RuleFor(x => x.Pais)
            .NotEmpty().WithMessage("El país es obligatorio")
            .MaximumLength(100).WithMessage("El país no puede exceder 100 caracteres");

        RuleFor(x => x.Departamento)
            .MaximumLength(100).WithMessage("El departamento no puede exceder 100 caracteres");

        RuleFor(x => x.Ciudad)
            .MaximumLength(100).WithMessage("La ciudad no puede exceder 100 caracteres");

        RuleFor(x => x.CodigoPostal)
            .MaximumLength(20).WithMessage("El código postal no puede exceder 20 caracteres");

        RuleFor(x => x.ContactoNombre)
            .MaximumLength(200).WithMessage("El nombre de contacto no puede exceder 200 caracteres");

        RuleFor(x => x.ContactoTelefono)
            .MaximumLength(50).WithMessage("El teléfono de contacto no puede exceder 50 caracteres");

        RuleFor(x => x.ContactoEmail)
            .EmailAddress().WithMessage("El email de contacto no tiene un formato válido")
            .MaximumLength(200).WithMessage("El email de contacto no puede exceder 200 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactoEmail));

        RuleFor(x => x.HorarioApertura)
            .Matches("^([01]\\d|2[0-3]):[0-5]\\d$").WithMessage("El horario de apertura debe tener formato HH:mm (24h)")
            .When(x => !string.IsNullOrWhiteSpace(x.HorarioApertura));

        RuleFor(x => x.HorarioCierre)
            .Matches("^([01]\\d|2[0-3]):[0-5]\\d$").WithMessage("El horario de cierre debe tener formato HH:mm (24h)")
            .When(x => !string.IsNullOrWhiteSpace(x.HorarioCierre));

        RuleFor(x => x.TiempoServicioMin)
            .InclusiveBetween(0, 600).WithMessage("El tiempo de servicio debe estar entre 0 y 600 minutos");

        RuleFor(x => x.Instrucciones)
            .MaximumLength(1000).WithMessage("Las instrucciones no pueden exceder 1000 caracteres");

        // ── Coordenadas manuales ─────────────────────────────────
        RuleFor(x => x.Latitud)
            .InclusiveBetween(-90, 90).WithMessage("La latitud debe estar entre -90 y 90")
            .When(x => x.Latitud.HasValue);

        RuleFor(x => x.Longitud)
            .InclusiveBetween(-180, 180).WithMessage("La longitud debe estar entre -180 y 180")
            .When(x => x.Longitud.HasValue);

        RuleFor(x => x)
            .Must(x => x.Latitud.HasValue == x.Longitud.HasValue)
            .WithMessage("Debe indicar latitud y longitud juntas (o ninguna)")
            .WithName("Coordenadas");
    }
}