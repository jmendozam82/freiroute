using FluentValidation;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

public class OrdenValidator : AbstractValidator<OrdenRequestDto>
{
    public OrdenValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty()
            .WithMessage("El cliente es obligatorio");

        RuleFor(x => x.OrigenId)
            .NotEmpty()
            .WithMessage("El origen es obligatorio");

        RuleFor(x => x.DestinoId)
            .NotEmpty()
            .WithMessage("El destino es obligatorio")
            .NotEqual(x => x.OrigenId)
            .WithMessage("El origen y el destino no pueden ser la misma ubicación");  // CA-07

        RuleFor(x => x.TipoMercanciaId)
            .NotEmpty()
            .WithMessage("El tipo de mercancía es obligatorio");

        RuleFor(x => x.UnidadMedidaId)
            .NotEmpty()
            .WithMessage("La unidad de medida es obligatoria");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0)
            .WithMessage("La cantidad debe ser mayor a cero");  // CA-05

        RuleFor(x => x.PesoKg)
            .GreaterThan(0)
            .WithMessage("El peso debe ser mayor a cero");  // CA-04

        RuleFor(x => x.VolumenM3)
            .GreaterThan(0)
            .WithMessage("El volumen debe ser mayor a cero")
            .When(x => x.VolumenM3.HasValue);

        RuleFor(x => x.ModoTransporte)
            .Must(v => new[]
            {
                "TERRESTRE", "AEREO", "MARITIMO", "FERROVIARIO", "INTERMODAL"
            }.Contains(v))
            .WithMessage(v =>
                $"'{v}' no es un modo de transporte válido. " +
                "Valores: TERRESTRE, AEREO, MARITIMO, FERROVIARIO, INTERMODAL");  // CA-09

        RuleFor(x => x.NivelServicio)
            .Must(v => new[]
            {
                NivelServicio.Estandar, NivelServicio.Express, NivelServicio.Programado
            }.Contains(v!))
            .WithMessage("Nivel de servicio no válido. Valores: ESTANDAR, EXPRESS, PROGRAMADO")
            .When(x => !string.IsNullOrEmpty(x.NivelServicio));  // CA-10

        RuleFor(x => x.Prioridad)
            .Must(v => new[]
            {
                OrdenPrioridad.Critico, OrdenPrioridad.Alto,
                OrdenPrioridad.Normal, OrdenPrioridad.Bajo
            }.Contains(v!))
            .WithMessage("Prioridad no válida. Valores: CRITICO, ALTO, NORMAL, BAJO")
            .When(x => !string.IsNullOrEmpty(x.Prioridad));  // CA-11

        // Fecha entrega >= fecha pickup cuando ambas están presentes (CA-06)
        RuleFor(x => x.FechaEntregaRequerida)
            .GreaterThanOrEqualTo(x => x.FechaPickupSolicitada)
            .WithMessage("La fecha de entrega debe ser posterior a la fecha de recogida")
            .When(x => x.FechaPickupSolicitada.HasValue
                     && x.FechaEntregaRequerida.HasValue);

        RuleFor(x => x.ReferenciaCliente)
            .MaximumLength(100)
            .WithMessage("La referencia del cliente no puede exceder 100 caracteres")
            .When(x => x.ReferenciaCliente != null);

        // Validar líneas si se envían
        RuleForEach(x => x.Lineas)
            .SetValidator(new LineaOrdenValidator())
            .When(x => x.Lineas != null);
    }
}

public class LineaOrdenValidator : AbstractValidator<LineaOrdenRequestDto>
{
    public LineaOrdenValidator()
    {
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción de la línea es obligatoria")
            .MaximumLength(255).WithMessage("La descripción no puede exceder 255 caracteres");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("La cantidad de la línea debe ser mayor a cero");

        RuleFor(x => x.PesoKg)
            .GreaterThan(0).When(x => x.PesoKg.HasValue)
            .WithMessage("El peso de la línea debe ser mayor a cero");
    }
}
