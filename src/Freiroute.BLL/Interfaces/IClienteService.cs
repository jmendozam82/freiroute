using Freiroute.DTO.Cliente;
using Freiroute.Utility.Pagination;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio del catálogo de clientes (shippers)
/// y sus contactos (HU-019). Todo método recibe empresaId extraído del
/// JWT — nunca del body del request (ADR-003).
/// </summary>
public interface IClienteService
{
    /// <summary>
    /// Lista paginada de clientes con filtros por tipo, estado de
    /// crédito y texto. Los clientes BLOQUEADO muestran alerta visual.
    /// </summary>
    Task<PagedResult<ClienteResponseDto>> GetAllAsync(Guid empresaId,
        string? tipoCliente, string? estadoCredito,
        string? q, int page, int pageSize);

    /// <summary>Obtiene un cliente con sus contactos por Id dentro de la empresa.</summary>
    Task<ClienteResponseDto?> GetByIdAsync(Guid id, Guid empresaId);

    /// <summary>
    /// Crea el cliente + contactos en una sola operación.
    /// Valida RUC/NIT único por empresa (HU-019 CA-08).
    /// </summary>
    Task<ClienteResponseDto> CreateAsync(
        ClienteRequestDto dto, Guid empresaId);

    /// <summary>Actualiza el cliente + contactos.</summary>
    Task<ClienteResponseDto> UpdateAsync(
        Guid id, ClienteRequestDto dto, Guid empresaId);

    /// <summary>Soft delete: activo = false (ADR-005).</summary>
    Task<bool> DeactivateAsync(Guid id, Guid empresaId);

    /// <summary>Cambia el estado de crédito del cliente (CA-05).</summary>
    Task<ClienteResponseDto> CambiarEstadoCreditoAsync(
        Guid id, string nuevoEstado, Guid empresaId);

    // ── Contactos ──────────────────────────────────────────────

    /// <summary>Agrega un contacto al cliente.</summary>
    Task<ContactoClienteResponseDto> AgregarContactoAsync(
        Guid clienteId, ContactoClienteRequestDto dto, Guid empresaId);

    /// <summary>Actualiza un contacto del cliente.</summary>
    Task<ContactoClienteResponseDto> UpdateContactoAsync(
        Guid clienteId, Guid contactoId,
        ContactoClienteRequestDto dto, Guid empresaId);

    /// <summary>Soft delete de un contacto: activo = false.</summary>
    Task<bool> DeactivateContactoAsync(
        Guid clienteId, Guid contactoId, Guid empresaId);

    /// <summary>
    /// Importación masiva CSV con validación de RUC único por empresa
    /// (HU-019 CA-08, ADR-017): filas válidas se importan, las inválidas
    /// se registran y continúa. Retorna el número de registros importados.
    /// </summary>
    Task<int> ImportarCsvAsync(Stream csv, Guid empresaId);

    /// <summary>Exporta el directorio de clientes a Excel (HU-019 CA-09).</summary>
    Task<byte[]> ExportarExcelAsync(Guid empresaId);
}