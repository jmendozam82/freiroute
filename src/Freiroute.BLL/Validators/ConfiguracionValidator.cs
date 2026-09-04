using FluentValidation;
using Freiroute.DTO.Configuracion;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de la configuración general del tenant (HU-014).
/// Valida identidad, colores del tema (HEX), moneda, zona horaria y formato de fecha.
/// </summary>
public class ConfiguracionValidator : AbstractValidator<ConfiguracionRequestDto>
{
    public ConfiguracionValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la empresa es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.RucNit)
            .MaximumLength(50).WithMessage("El RUC/NIT no puede exceder 50 caracteres");

        RuleFor(x => x.Direccion)
            .MaximumLength(500).WithMessage("La dirección no puede exceder 500 caracteres");

        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres");

        RuleFor(x => x.Industria)
            .MaximumLength(100).WithMessage("La industria no puede exceder 100 caracteres");

        RuleFor(x => x.SitioWeb)
            .Must(EsUrlValida).WithMessage("El sitio web no tiene un formato válido")
            .When(x => !string.IsNullOrWhiteSpace(x.SitioWeb));

        RuleFor(x => x.ColorPrimario)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color primario debe ser un hex válido (#RRGGBB)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorPrimario));

        RuleFor(x => x.ColorSecundario)
            .Matches("^#[0-9A-Fa-f]{6}$").WithMessage("El color secundario debe ser un hex válido (#RRGGBB)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorSecundario));

        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria")
            .Length(3).WithMessage("La moneda debe ser el código ISO 4217 de 3 letras");

        RuleFor(x => x.ZonaHoraria)
            .NotEmpty().WithMessage("La zona horaria es obligatoria")
            .MaximumLength(100).WithMessage("La zona horaria no puede exceder 100 caracteres");

        RuleFor(x => x.FormatoFecha)
            .Must(f => f == "DD/MM/YYYY" || f == "MM/DD/YYYY" || f == "YYYY/MM/DD")
            .WithMessage("El formato de fecha debe ser DD/MM/YYYY, MM/DD/YYYY o YYYY/MM/DD")
            .When(x => !string.IsNullOrWhiteSpace(x.FormatoFecha));

        RuleFor(x => x.EmailRemitente)
            .EmailAddress().WithMessage("El email remitente no tiene un formato válido")
            .When(x => !string.IsNullOrWhiteSpace(x.EmailRemitente));

        RuleFor(x => x.NombreRemitente)
            .MaximumLength(200).WithMessage("El nombre del remitente no puede exceder 200 caracteres");
    }

    private static bool EsUrlValida(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
