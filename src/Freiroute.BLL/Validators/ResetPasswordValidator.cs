using FluentValidation;
using Freiroute.DTO.Auth;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación del reset de contraseña (HU-007 CA-05).
/// La nueva contraseña debe cumplir los mismos requisitos que el registro:
/// mínimo 8 caracteres, 1 mayúscula, 1 número y 1 carácter especial.
/// Mensajes específicos por cada regla fallida (feedback claro al usuario).
/// </summary>
public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token es obligatorio");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Must(ContenerMayuscula).WithMessage("La contraseña debe contener al menos una mayúscula")
            .Must(ContenerNumero).WithMessage("La contraseña debe contener al menos un número")
            .Must(ContenerEspecial).WithMessage("La contraseña debe contener al menos un carácter especial");
    }

    private static bool ContenerMayuscula(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsUpper);

    private static bool ContenerNumero(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(char.IsDigit);

    private static bool ContenerEspecial(string password) =>
        !string.IsNullOrEmpty(password) && password.Any(c => !char.IsLetterOrDigit(c));
}