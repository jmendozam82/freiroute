using FluentValidation;
using Freiroute.DTO.Auth;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación del login (HU-003). Email obligatorio con formato válido;
/// contraseña obligatoria con mínimo 8 caracteres (HU-003 CA-03).
/// </summary>
public class LoginValidator : AbstractValidator<LoginRequestDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El email no tiene un formato válido")
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres");
    }
}