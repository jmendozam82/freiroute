using FluentValidation;
using Freiroute.DTO.Cliente;
using Freiroute.Utility.Constants;

namespace Freiroute.BLL.Validators;

/// <summary>
/// Validación de clientes (shippers) y sus contactos (HU-019).
/// Valida identificación fiscal, configuración comercial/crédito y que
/// los contactos tengan a lo sumo un contacto principal.
/// </summary>
public class ClienteValidator : AbstractValidator<ClienteRequestDto>
{
    private static readonly string[] TiposDocumento = { "RUC", "NIT", "CEDULA", "PASAPORTE" };

    public ClienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre o razón social del cliente es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.NombreComercial)
            .MaximumLength(200).WithMessage("El nombre comercial no puede exceder 200 caracteres");

        RuleFor(x => x.RucNit)
            .NotEmpty().WithMessage("El RUC/NIT es obligatorio")
            .MaximumLength(50).WithMessage("El RUC/NIT no puede exceder 50 caracteres")
            .Matches("^[A-Za-z0-9-]+$").WithMessage("El RUC/NIT solo admite letras, números y guiones");

        RuleFor(x => x.TipoDocumento)
            .NotEmpty().WithMessage("El tipo de documento es obligatorio")
            .Must(t => TiposDocumento.Contains(t))
            .WithMessage("El tipo de documento debe ser RUC, NIT, CEDULA o PASAPORTE");

        RuleFor(x => x.TipoCliente)
            .NotEmpty().WithMessage("El tipo de cliente es obligatorio")
            .Must(t => TipoCliente.Todos.Contains(t))
            .WithMessage("El tipo de cliente no es válido");

        RuleFor(x => x.Industria)
            .MaximumLength(100).WithMessage("La industria no puede exceder 100 caracteres");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no tiene un formato válido")
            .MaximumLength(200).WithMessage("El email no puede exceder 200 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede exceder 50 caracteres");

        RuleFor(x => x.SitioWeb)
            .Must(EsUrlValida).WithMessage("El sitio web no tiene un formato válido")
            .When(x => !string.IsNullOrWhiteSpace(x.SitioWeb));

        RuleFor(x => x.DireccionFiscal)
            .MaximumLength(500).WithMessage("La dirección fiscal no puede exceder 500 caracteres");

        RuleFor(x => x.Pais)
            .NotEmpty().WithMessage("El país es obligatorio")
            .MaximumLength(100).WithMessage("El país no puede exceder 100 caracteres");

        RuleFor(x => x.Departamento)
            .MaximumLength(100).WithMessage("El departamento no puede exceder 100 caracteres");

        RuleFor(x => x.Ciudad)
            .MaximumLength(100).WithMessage("La ciudad no puede exceder 100 caracteres");

        // ── Configuración comercial / crédito ────────────────────
        RuleFor(x => x.CreditoDias)
            .InclusiveBetween(0, 365).WithMessage("Los días de crédito deben estar entre 0 y 365");

        RuleFor(x => x.LimiteCredito)
            .GreaterThanOrEqualTo(0).WithMessage("El límite de crédito no puede ser negativo");

        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria")
            .Length(3).WithMessage("La moneda debe ser el código ISO 4217 de 3 letras");

        RuleFor(x => x.SlaDiasEntrega)
            .InclusiveBetween(1, 365).WithMessage("Los días de entrega SLA deben estar entre 1 y 365")
            .When(x => x.SlaDiasEntrega.HasValue);

        // ── Contactos ────────────────────────────────────────────
        RuleForEach(x => x.Contactos).ChildRules(contacto =>
        {
            contacto.RuleFor(c => c.Nombre)
                .NotEmpty().WithMessage("El nombre del contacto es obligatorio")
                .MaximumLength(200).WithMessage("El nombre del contacto no puede exceder 200 caracteres");

            contacto.RuleFor(c => c.Cargo)
                .MaximumLength(100).WithMessage("El cargo no puede exceder 100 caracteres");

            contacto.RuleFor(c => c.Rol)
                .Must(r => RolContacto.Todos.Contains(r))
                .WithMessage("El rol del contacto no es válido");

            contacto.RuleFor(c => c.Email)
                .EmailAddress().WithMessage("El email del contacto no tiene un formato válido")
                .MaximumLength(200).WithMessage("El email del contacto no puede exceder 200 caracteres")
                .When(c => !string.IsNullOrWhiteSpace(c.Email));

            contacto.RuleFor(c => c.Telefono)
                .MaximumLength(50).WithMessage("El teléfono del contacto no puede exceder 50 caracteres");
        });

        RuleFor(x => x.Contactos)
            .Must(contactos => contactos.Count(c => c.EsPrincipal) <= 1)
            .WithMessage("Solo puede haber un contacto principal por cliente");
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