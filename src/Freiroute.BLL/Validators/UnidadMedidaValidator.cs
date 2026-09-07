using FluentValidation;
using Freiroute.DTO.Unidad;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de unidades de medida (HU-018). Valida identificación,
/// tipo (PESO, VOLUMEN, LONGITUD, TEMPERATURA), factor de conversión
/// positivo y coherencia entre tipo y unidad base (kg, m3, m, C).
/// </summary>
public class UnidadMedidaValidator : AbstractValidator<UnidadMedidaRequestDto>
{
    private static readonly string[] TiposValidos =
    {
        TipoMedida.Peso, TipoMedida.Volumen, TipoMedida.Longitud, TipoMedida.Temperatura
    };

    public UnidadMedidaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la unidad es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

        RuleFor(x => x.Simbolo)
            .NotEmpty().WithMessage("El símbolo de la unidad es obligatorio")
            .MaximumLength(20).WithMessage("El símbolo no puede exceder 20 caracteres");

        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de unidad es obligatorio")
            .Must(t => TiposValidos.Contains(t))
            .WithMessage("El tipo de unidad debe ser PESO, VOLUMEN, LONGITUD o TEMPERATURA");

        // TEMPERATURA usa desplazamiento (0°C = 273.15K ≠ 0K): no aplica factor lineal.
        RuleFor(x => x.FactorConversion)
            .GreaterThan(0).WithMessage("El factor de conversión debe ser mayor que cero");

        RuleFor(x => x.UnidadBase)
            .NotEmpty().WithMessage("La unidad base es obligatoria")
            .Must((dto, baseU) => UnidadBaseCoincideConTipo(dto.Tipo, baseU))
            .WithMessage("La unidad base no corresponde al tipo de unidad (kg, m3, m, C)");
    }

    private static bool UnidadBaseCoincideConTipo(string tipo, string unidadBase)
    {
        var baseEsperada = tipo switch
        {
            TipoMedida.Peso => TipoMedida.BaseKg,
            TipoMedida.Volumen => TipoMedida.BaseM3,
            TipoMedida.Longitud => TipoMedida.BaseM,
            TipoMedida.Temperatura => TipoMedida.BaseC,
            _ => null
        };

        return baseEsperada is not null &&
               string.Equals(baseEsperada, unidadBase, System.StringComparison.OrdinalIgnoreCase);
    }
}