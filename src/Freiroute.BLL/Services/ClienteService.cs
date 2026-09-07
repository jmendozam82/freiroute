using System.Text;
using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Cliente;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Freiroute.Utility.Pagination;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio del catálogo de clientes (shippers) y sus contactos
/// (HU-019). RUC/NIT único por empresa (CA-08); los bloquedos muestran
/// alerta visual. Exportación CSV con BOM UTF-8 (CA-09, ADR-017).
/// </summary>
public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IUbicacionRepository _ubicacionRepository;
    private readonly IValidator<ClienteRequestDto> _validator;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(
        IClienteRepository clienteRepository,
        IUbicacionRepository ubicacionRepository,
        IValidator<ClienteRequestDto> validator,
        IAuditoriaService auditoria,
        ILogger<ClienteService> logger)
    {
        _clienteRepository = clienteRepository;
        _ubicacionRepository = ubicacionRepository;
        _validator = validator;
        _auditoria = auditoria;
        _logger = logger;
    }

    /// <summary>Lista paginada de clientes con filtros (HU-019 CA-01).</summary>
    public async Task<PagedResult<ClienteResponseDto>> GetAllAsync(
        Guid empresaId, string? tipoCliente, string? estadoCredito,
        string? q, int page, int pageSize)
    {
        var todos = await _clienteRepository.GetAllAsync(
            empresaId, tipoCliente, estadoCredito, q);

        var pageNumber = Math.Max(page, 1);
        var size = pageSize <= 0 ? 20 : pageSize;

        var items = new List<ClienteResponseDto>();
        foreach (var cliente in todos.Skip((pageNumber - 1) * size).Take(size))
        {
            items.Add(await MapToResponse(cliente, empresaId));
        }

        return new PagedResult<ClienteResponseDto>
        {
            Items = items,
            TotalItems = todos.Count(),
            PageNumber = pageNumber,
            PageSize = size
        };
    }

    /// <summary>Obtiene un cliente con sus contactos por Id dentro de la empresa.</summary>
    public async Task<ClienteResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id, empresaId);
        return cliente is null ? null : await MapToResponse(cliente, empresaId);
    }

    /// <summary>
    /// Crea el cliente + contactos en una sola operación.
    /// Valida RUC/NIT único por empresa (HU-019 CA-08).
    /// </summary>
    public async Task<ClienteResponseDto> CreateAsync(
        ClienteRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        await ValidarRucUnicoAsync(dto.RucNit, empresaId, excludeId: null);

        var cliente = MapToEntity(dto, empresaId);
        var clienteId = await _clienteRepository.CreateAsync(cliente);

        foreach (var contactoDto in dto.Contactos)
        {
            var contacto = MapContactoToEntity(contactoDto, clienteId, empresaId);
            await _clienteRepository.CreateContactoAsync(contacto);
        }

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.CREATE, empresaId, null,
            "Cliente", clienteId, new { nombre = cliente.Nombre, rucNit = cliente.RucNit });

        return await GetByIdAsync(clienteId, empresaId)
               ?? throw new NotFoundException("clientes", clienteId);
    }

    /// <summary>Actualiza el cliente + contactos (posiciónal: reemplaza el set).</summary>
    public async Task<ClienteResponseDto> UpdateAsync(
        Guid id, ClienteRequestDto dto, Guid empresaId)
    {
        await ValidarAsync(dto);

        var existente = await _clienteRepository.GetByIdAsync(id, empresaId)
                        ?? throw new NotFoundException("clientes", id);

        await ValidarRucUnicoAsync(dto.RucNit, empresaId, excludeId: id);

        var cliente = MapToEntity(dto, empresaId);
        cliente.Id = id;
        cliente.EstadoCredito = existente.EstadoCredito;
        cliente.FechaCreacion = existente.FechaCreacion;

        var ok = await _clienteRepository.UpdateAsync(cliente);
        if (!ok)
        {
            throw new NotFoundException("clientes", id);
        }

        // Contactos: posiciónal — se actualizan los existentes, se crean los
        // nuevos y se desactivan los que sobren (sin DELETE físico, ADR-005).
        var existentes = (await _clienteRepository.GetContactosAsync(id, empresaId)).ToList();
        for (var i = 0; i < dto.Contactos.Count; i++)
        {
            var contactoDto = dto.Contactos[i];
            if (i < existentes.Count)
            {
                var contacto = MapContactoToEntity(contactoDto, id, empresaId);
                contacto.Id = existentes[i].Id;
                contacto.FechaCreacion = existentes[i].FechaCreacion;
                await _clienteRepository.UpdateContactoAsync(contacto);
            }
            else
            {
                var contacto = MapContactoToEntity(contactoDto, id, empresaId);
                await _clienteRepository.CreateContactoAsync(contacto);
            }
        }

        for (var i = dto.Contactos.Count; i < existentes.Count; i++)
        {
            await _clienteRepository.DeactivateContactoAsync(existentes[i].Id, empresaId);
        }

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.UPDATE, empresaId, null,
            "Cliente", id, new { nombre = cliente.Nombre });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("clientes", id);
    }

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id, empresaId)
                      ?? throw new NotFoundException("clientes", id);

        var ok = await _clienteRepository.DeactivateAsync(id, empresaId);

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.DEACTIVATE, empresaId, null,
            "Cliente", id, new { nombre = cliente.Nombre });

        return ok;
    }

    /// <summary>Cambia el estado de crédito del cliente (CA-05). BLOQUEADO alerta en toda la UI.</summary>
    public async Task<ClienteResponseDto> CambiarEstadoCreditoAsync(
        Guid id, string nuevoEstado, Guid empresaId)
    {
        var estadosValidos = new[] { EstadoCredito.AlDia, EstadoCredito.EnMora, EstadoCredito.Bloqueado, EstadoCredito.SinCredito };
        if (!estadosValidos.Contains(nuevoEstado))
        {
            throw new BusinessException("El estado de crédito no es válido.", "CLIENTE_ESTADO_CREDITO_INVALIDO");
        }

        var cliente = await _clienteRepository.GetByIdAsync(id, empresaId)
                      ?? throw new NotFoundException("clientes", id);

        var ok = await _clienteRepository.ActualizarEstadoCreditoAsync(id, nuevoEstado, empresaId);
        if (!ok)
        {
            throw new NotFoundException("clientes", id);
        }

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.CAMBIO_ESTADO, empresaId, null,
            "Cliente", id, new { anterior = cliente.EstadoCredito, nuevo = nuevoEstado });

        return await GetByIdAsync(id, empresaId)
               ?? throw new NotFoundException("clientes", id);
    }

    // ── Contactos ──────────────────────────────────────────────

    /// <summary>Agrega un contacto al cliente.</summary>
    public async Task<ContactoClienteResponseDto> AgregarContactoAsync(
        Guid clienteId, ContactoClienteRequestDto dto, Guid empresaId)
    {
        var cliente = await _clienteRepository.GetByIdAsync(clienteId, empresaId)
                      ?? throw new NotFoundException("clientes", clienteId);

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new BusinessException("El nombre del contacto es obligatorio.", "CONTACTO_NOMBRE_REQUERIDO");
        }

        var contacto = MapContactoToEntity(dto, clienteId, empresaId);
        var contactoId = await _clienteRepository.CreateContactoAsync(contacto);

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.CREATE, empresaId, null,
            "ContactoCliente", contactoId, new { clienteId, nombre = contacto.Nombre });

        return await GetContactoResponseAsync(clienteId, contactoId, empresaId)
               ?? throw new NotFoundException("contactos_cliente", contactoId);
    }

    /// <summary>Actualiza un contacto del cliente.</summary>
    public async Task<ContactoClienteResponseDto> UpdateContactoAsync(
        Guid clienteId, Guid contactoId,
        ContactoClienteRequestDto dto, Guid empresaId)
    {
        var cliente = await _clienteRepository.GetByIdAsync(clienteId, empresaId)
                      ?? throw new NotFoundException("clientes", clienteId);

        var contactos = await _clienteRepository.GetContactosAsync(clienteId, empresaId);
        var existente = contactos.FirstOrDefault(c => c.Id == contactoId)
                        ?? throw new NotFoundException("contactos_cliente", contactoId);

        var contacto = MapContactoToEntity(dto, clienteId, empresaId);
        contacto.Id = contactoId;
        contacto.FechaCreacion = existente.FechaCreacion;

        var ok = await _clienteRepository.UpdateContactoAsync(contacto);
        if (!ok)
        {
            throw new NotFoundException("contactos_cliente", contactoId);
        }

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.UPDATE, empresaId, null,
            "ContactoCliente", contactoId, new { clienteId });

        return await GetContactoResponseAsync(clienteId, contactoId, empresaId)
               ?? throw new NotFoundException("contactos_cliente", contactoId);
    }

    /// <summary>Soft delete de un contacto: activo = false.</summary>
    public async Task<bool> DeactivateContactoAsync(
        Guid clienteId, Guid contactoId, Guid empresaId)
    {
        var cliente = await _clienteRepository.GetByIdAsync(clienteId, empresaId)
                      ?? throw new NotFoundException("clientes", clienteId);

        var ok = await _clienteRepository.DeactivateContactoAsync(contactoId, empresaId);
        if (!ok)
        {
            throw new NotFoundException("contactos_cliente", contactoId);
        }

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.DEACTIVATE, empresaId, null,
            "ContactoCliente", contactoId, new { clienteId });

        return ok;
    }

    /// <summary>
    /// Importación masiva CSV (HU-019 CA-08, ADR-017). Columnas:
    /// Nombre;RUC;Tipo;Email;Teléfono;Ciudad;Crédito días;Límite crédito;Estado crédito.
    /// Las filas inválidas o con RUC duplicado se registran y la importación continúa.
    /// </summary>
    public async Task<int> ImportarCsvAsync(Stream csv, Guid empresaId)
    {
        var importados = 0;
        var fila = 0;

        using var reader = new StreamReader(csv, Encoding.UTF8);
        while (await reader.ReadLineAsync() is { } linea)
        {
            fila++;
            if (fila == 1)
            {
                continue; // Cabecera.
            }

            if (string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            try
            {
                var campos = linea.Split(';');
                if (campos.Length < 2 || string.IsNullOrWhiteSpace(campos[0]))
                {
                    _logger.LogWarning("CSV clientes: fila {Fila} omitida (datos incompletos)", fila);
                    continue;
                }

                var dto = new ClienteRequestDto
                {
                    Nombre = campos[0].Trim(),
                    RucNit = string.IsNullOrWhiteSpace(campos[1]) ? null : campos[1].Trim(),
                    TipoCliente = string.IsNullOrWhiteSpace(campos[2]) ? TipoCliente.Regular : campos[2].Trim(),
                    Email = string.IsNullOrWhiteSpace(campos[3]) ? null : campos[3].Trim(),
                    Telefono = string.IsNullOrWhiteSpace(campos[4]) ? null : campos[4].Trim(),
                    Ciudad = string.IsNullOrWhiteSpace(campos[5]) ? null : campos[5].Trim(),
                    CreditoDias = IntOpcional(campos, 6) ?? 0,
                    LimiteCredito = DecimalOpcional(campos, 7) ?? 0m
                };

                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                {
                    _logger.LogWarning(
                        "CSV clientes: fila {Fila} omitida ({Errores})",
                        fila, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                    continue;
                }

                if (dto.RucNit is not null &&
                    await _clienteRepository.ExisteRucAsync(dto.RucNit, empresaId, null))
                {
                    _logger.LogWarning(
                        "CSV clientes: fila {Fila} omitida (RUC '{Ruc}' ya existe en la empresa)",
                        fila, dto.RucNit);
                    continue;
                }

                await _clienteRepository.CreateAsync(MapToEntity(dto, empresaId));
                importados++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CSV clientes: fila {Fila} omitida por error inesperado", fila);
            }
        }

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.CREATE, empresaId, null,
            "Cliente", null, new { importados, archivo = "csv" });

        return importados;
    }

    /// <summary>
    /// Exporta el directorio de clientes a CSV compatible con Excel
    /// (CA-09, ADR-017): BOM UTF-8, separador ';' y cabecera fija.
    /// </summary>
    public async Task<byte[]> ExportarExcelAsync(Guid empresaId)
    {
        var clientes = await _clienteRepository.GetAllAsync(empresaId);

        var sb = new StringBuilder();
        sb.AppendLine("Nombre;RUC;Tipo;Email;Teléfono;Ciudad;Crédito días;Límite crédito;Estado crédito");

        foreach (var c in clientes)
        {
            sb.AppendLine(string.Join(';',
                Csv(c.Nombre),
                Csv(c.RucNit),
                c.TipoCliente,
                Csv(c.Email),
                Csv(c.Telefono),
                Csv(c.Ciudad),
                c.CreditoDias.ToString(),
                c.LimiteCredito.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                c.EstadoCredito));
        }

        await _auditoria.RegistrarAsync(
            "clientes", AccionAuditoria.EXPORT, empresaId, null,
            "Cliente", null, new { registros = clientes.Count(), formato = "csv" });

        // BOM UTF-8 para que Excel detecte acentos correctamente (ADR-017).
        var bom = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return bom;
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task ValidarAsync(ClienteRequestDto dto)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }
    }

    private async Task ValidarRucUnicoAsync(string? rucNit, Guid empresaId, Guid? excludeId)
    {
        if (string.IsNullOrWhiteSpace(rucNit))
        {
            return;
        }

        if (await _clienteRepository.ExisteRucAsync(rucNit.Trim(), empresaId, excludeId))
        {
            throw new ConflictException(
                $"Ya existe un cliente con el RUC/NIT '{rucNit}' en esta empresa.");
        }
    }

    private async Task<ClienteResponseDto> MapToResponse(Cliente c, Guid empresaId)
    {
        var contactos = await _clienteRepository.GetContactosAsync(c.Id, empresaId);

        string? ubicacionNombre = null;
        if (c.UbicacionDefectoId.HasValue)
        {
            var ubicacion = await _ubicacionRepository.GetByIdAsync(c.UbicacionDefectoId.Value, empresaId);
            ubicacionNombre = ubicacion?.Nombre;
        }

        return new ClienteResponseDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            NombreComercial = c.NombreComercial,
            RucNit = c.RucNit,
            TipoCliente = c.TipoCliente,
            TipoClienteLabel = TipoClienteLabel(c.TipoCliente),
            Industria = c.Industria,
            Email = c.Email,
            Telefono = c.Telefono,
            DireccionFiscal = c.DireccionFiscal,
            Pais = c.Pais,
            Ciudad = c.Ciudad,
            UbicacionDefectoId = c.UbicacionDefectoId,
            UbicacionDefectoNombre = ubicacionNombre,
            CreditoDias = c.CreditoDias,
            LimiteCredito = c.LimiteCredito,
            Moneda = c.Moneda,
            EstadoCredito = c.EstadoCredito,
            EstadoCreditoLabel = EstadoCreditoLabel(c.EstadoCredito),
            SlaDiasEntrega = c.SlaDiasEntrega,
            Contactos = contactos.Select(MapContactoToResponse).ToList(),
            Activo = c.Activo,
            FechaCreacion = c.FechaCreacion
        };
    }

    private async Task<ContactoClienteResponseDto?> GetContactoResponseAsync(
        Guid clienteId, Guid contactoId, Guid empresaId)
    {
        var contactos = await _clienteRepository.GetContactosAsync(clienteId, empresaId);
        var contacto = contactos.FirstOrDefault(c => c.Id == contactoId);
        return contacto is null ? null : MapContactoToResponse(contacto);
    }

    private static Cliente MapToEntity(ClienteRequestDto dto, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        Nombre = dto.Nombre,
        NombreComercial = string.IsNullOrWhiteSpace(dto.NombreComercial) ? null : dto.NombreComercial.Trim(),
        RucNit = string.IsNullOrWhiteSpace(dto.RucNit) ? null : dto.RucNit.Trim(),
        TipoDocumento = dto.TipoDocumento,
        TipoCliente = dto.TipoCliente,
        Industria = string.IsNullOrWhiteSpace(dto.Industria) ? null : dto.Industria.Trim(),
        Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
        Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
        SitioWeb = string.IsNullOrWhiteSpace(dto.SitioWeb) ? null : dto.SitioWeb.Trim(),
        DireccionFiscal = string.IsNullOrWhiteSpace(dto.DireccionFiscal) ? null : dto.DireccionFiscal.Trim(),
        Pais = dto.Pais,
        Departamento = string.IsNullOrWhiteSpace(dto.Departamento) ? null : dto.Departamento.Trim(),
        Ciudad = string.IsNullOrWhiteSpace(dto.Ciudad) ? null : dto.Ciudad.Trim(),
        UbicacionDefectoId = dto.UbicacionDefectoId,
        CreditoDias = dto.CreditoDias,
        LimiteCredito = dto.LimiteCredito,
        Moneda = dto.Moneda,
        EstadoCredito = EstadoCredito.AlDia,
        SlaDiasEntrega = dto.SlaDiasEntrega,
        Activo = true
    };

    private static ContactoCliente MapContactoToEntity(
        ContactoClienteRequestDto dto, Guid clienteId, Guid empresaId) => new()
    {
        Id = Guid.Empty, // La BD genera el UUID (regla 11 AGENTS.md).
        EmpresaId = empresaId,
        ClienteId = clienteId,
        Nombre = dto.Nombre,
        Cargo = string.IsNullOrWhiteSpace(dto.Cargo) ? null : dto.Cargo.Trim(),
        Rol = dto.Rol,
        Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
        Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
        EsPrincipal = dto.EsPrincipal,
        Activo = true
    };

    private static ContactoClienteResponseDto MapContactoToResponse(ContactoCliente c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Cargo = c.Cargo,
        Rol = c.Rol,
        RolLabel = RolLabel(c.Rol),
        Email = c.Email,
        Telefono = c.Telefono,
        EsPrincipal = c.EsPrincipal,
        Activo = c.Activo
    };

    private static string Csv(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : valor.Trim().Replace(";", ",");

    private static int? IntOpcional(string[] campos, int indice) =>
        campos.Length > indice && int.TryParse(campos[indice], out var result) ? result : null;

    private static decimal? DecimalOpcional(string[] campos, int indice) =>
        campos.Length > indice && decimal.TryParse(campos[indice],
            System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture,
            out var result)
            ? result
            : null;

    private static string TipoClienteLabel(string tipo) => tipo switch
    {
        TipoCliente.Vip => "VIP",
        TipoCliente.Ocasional => "Ocasional",
        TipoCliente.Corporativo => "Corporativo",
        TipoCliente.Gobierno => "Gobierno",
        _ => "Regular"
    };

    private static string EstadoCreditoLabel(string estado) => estado switch
    {
        EstadoCredito.EnMora => "En mora",
        EstadoCredito.Bloqueado => "Bloqueado",
        EstadoCredito.SinCredito => "Sin crédito",
        _ => "Al día"
    };

    private static string RolLabel(string rol) => rol switch
    {
        RolContacto.Logistica => "Logística",
        RolContacto.Compras => "Compras",
        RolContacto.Finanzas => "Finanzas",
        RolContacto.Recepcion => "Recepción",
        RolContacto.Gerencia => "Gerencia",
        _ => "General"
    };
}