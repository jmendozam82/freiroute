using FluentValidation;
using Freiroute.DTO.Permiso;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación del reemplazo de permisos de un perfil (HU-006, ADR-009).
/// - PerfilId obligatorio.
/// - La lista de módulos no puede estar vacía.
/// - Cada módulo debe ser uno de los valores de Constants.ModuloPermiso.
/// - Cada módulo debe tener al menos un flag activo (leer/crear/actualizar).
/// </summary>
public class PermisoValidator : AbstractValidator<PermisoRequestDto>
{
    public PermisoValidator()
    {
        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("El perfil es obligatorio");

        RuleFor(x => x.Modulos)
            .NotEmpty().WithMessage("Debe configurar al menos un módulo")
            .Must(modulos => modulos.Count > 0).WithMessage("Debe configurar al menos un módulo");

        RuleForEach(x => x.Modulos)
            .ChildRules(modulo =>
            {
                modulo.RuleFor(m => m.Modulo)
                    .NotEmpty().WithMessage("El módulo es obligatorio")
                    .Must(mod => ModuloPermiso.Todos.Contains(mod))
                    .WithMessage("El módulo '{PropertyValue}' no es un módulo válido del TMS");

                modulo.RuleFor(m => m)
                    .Must(m => m.PuedeLeer || m.PuedeCrear || m.PuedeActualizar)
                    .WithMessage("Cada módulo debe tener al menos un permiso activo (leer, crear o actualizar)");
            });
    }
}