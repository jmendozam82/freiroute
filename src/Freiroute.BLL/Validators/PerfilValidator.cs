using FluentValidation;
using Freiroute.DTO.Perfil;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de perfiles/roles (HU-006). TipoPerfil debe ser uno de los
/// valores de Constants.TipoPerfil (ADMIN, DISPATCHER, OPERADOR, CONDUCTOR,
/// CLIENTE, CUSTOM — nunca SUPER_ADMIN desde el cliente).
/// </summary>
public class PerfilValidator : AbstractValidator<PerfilRequestDto>
{
    private static readonly string[] TiposValidos =
    [
        TipoPerfil.ADMIN,
        TipoPerfil.DISPATCHER,
        TipoPerfil.OPERADOR,
        TipoPerfil.CONDUCTOR,
        TipoPerfil.CLIENTE,
        TipoPerfil.CUSTOM
    ];

    public PerfilValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del perfil es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.TipoPerfil)
            .Must(tipo => TiposValidos.Contains(tipo))
            .WithMessage("El tipo de perfil no es válido")
            .When(x => !string.IsNullOrWhiteSpace(x.TipoPerfil));
    }
}