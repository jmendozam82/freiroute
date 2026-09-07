using FluentValidation;
using Freiroute.DTO.Tarifa;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de tarifas base y sus recargos (HU-020, ADR-015).
/// Valida aplicación (zona/modo/servicio), modelo de precio, vigencia
/// y coherencia de los recargos (porcentaje 0-100 o monto fijo ≥ 0).
/// </summary>
public class TarifaBaseValidator : AbstractValidator<TarifaBaseRequestDto>
{
    public TarifaBaseValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la tarifa es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.Codigo)
            .MaximumLength(50).WithMessage("El código no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Codigo));

        RuleFor(x => x.ModoTransporte)
            .NotEmpty().WithMessage("El modo de transporte es obligatorio")
            .Must(m => ModoTransporte.Todos.Contains(m))
            .WithMessage("El modo de transporte no es válido");

        RuleFor(x => x.TipoServicio)
            .NotEmpty().WithMessage("El tipo de servicio es obligatorio")
            .Must(t => TipoServicioTransporte.Todos.Contains(t))
            .WithMessage("El tipo de servicio no es válido");

        RuleFor(x => x.TipoTarifa)
            .NotEmpty().WithMessage("El tipo de tarifa es obligatorio")
            .Must(t => TipoTarifa.Todos.Contains(t))
            .WithMessage("El tipo de tarifa no es válido");

        // ADR-015: FIJO_VIAJE no puede depender de unidad — la BLL no recibe unidades.
        RuleFor(x => x.TipoTarifa)
            .Must(t => t != TipoTarifa.PorUnidad)
            .WithMessage("El tipo de tarifa POR_UNIDAD no está disponible en esta versión");

        RuleFor(x => x.PrecioUnitario)
            .GreaterThan(0).WithMessage("El precio unitario debe ser mayor que cero");

        RuleFor(x => x.PrecioMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("El precio mínimo no puede ser negativo")
            .When(x => x.PrecioMinimo.HasValue);

        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria")
            .Length(3).WithMessage("La moneda debe ser el código ISO 4217 de 3 letras");

        RuleFor(x => x.FechaVigenciaDesde)
            .NotEmpty().WithMessage("La fecha de inicio de vigencia es obligatoria");

        RuleFor(x => x.FechaVigenciaHasta)
            .GreaterThan(x => x.FechaVigenciaDesde)
            .WithMessage("La fecha de fin de vigencia debe ser posterior a la de inicio")
            .When(x => x.FechaVigenciaHasta.HasValue);

        // ── Recargos (ADR-015) ───────────────────────────────────
        RuleForEach(x => x.Recargos).ChildRules(recargo =>
        {
            recargo.RuleFor(r => r.CodigoRecargo)
                .NotEmpty().WithMessage("El código del recargo es obligatorio")
                .Must(c => CodigoRecargo.Todos.Contains(c))
                .WithMessage("El código del recargo no es válido");

            recargo.RuleFor(r => r.Nombre)
                .NotEmpty().WithMessage("El nombre del recargo es obligatorio")
                .MaximumLength(200).WithMessage("El nombre del recargo no puede exceder 200 caracteres");

            recargo.RuleFor(r => r.TipoCalculo)
                .NotEmpty().WithMessage("El tipo de cálculo es obligatorio")
                .Must(t => t is TipoCalculoRecargo.Porcentaje or TipoCalculoRecargo.MontoFijo)
                .WithMessage("El tipo de cálculo debe ser PORCENTAJE o MONTO_FIJO");

            recargo.RuleFor(r => r.Valor)
                .GreaterThan(0).WithMessage("El valor del recargo debe ser mayor que cero");

            recargo.RuleFor(r => r.Valor)
                .LessThanOrEqualTo(100).WithMessage("Un porcentaje no puede exceder 100%")
                .When(r => r.TipoCalculo == TipoCalculoRecargo.Porcentaje);
        });

        RuleFor(x => x.Recargos)
            .Must(recargos => recargos.Select(r => r.CodigoRecargo).Distinct().Count() == recargos.Count)
            .WithMessage("No puede repetir el mismo código de recargo en una tarifa");
    }
}