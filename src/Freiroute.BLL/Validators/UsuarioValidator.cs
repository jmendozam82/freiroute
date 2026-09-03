using FluentValidation;
using Freiroute.DTO.Usuario;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de creación/actualización de usuarios por tenant (HU-003).
/// TipoIdentidad: CEDULA | PASAPORTE | RUC | DNI (spec Sprint 1).
/// </summary>
public class UsuarioValidator : AbstractValidator<UsuarioRequestDto>
{
    private static readonly string[] TiposIdentidad = ["CEDULA", "PASAPORTE", "RUC", "DNI"];

    public UsuarioValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El email no tiene un formato válido")
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres");

        RuleFor(x => x.TipoIdentidad)
            .Must(tipo => TiposIdentidad.Contains(tipo))
            .WithMessage("El tipo de identidad debe ser CEDULA, PASAPORTE, RUC o DNI")
            .When(x => !string.IsNullOrWhiteSpace(x.TipoIdentidad));

        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("El perfil es obligatorio");

        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));
    }
}