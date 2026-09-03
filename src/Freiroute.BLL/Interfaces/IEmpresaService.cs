using Freiroute.DTO.Empresa;

namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de la lógica de negocio de empresas/tenants (HU-001).
/// Es la tabla raíz del SaaS: NO recibe empresaId (la empresa se crea o se gestiona
/// globalmente por el SUPER_ADMIN). Al crear un tenant, el servicio transacciona:
/// empresa + perfiles base (ADMIN, DISPATCHER, OPERADOR, CONDUCTOR, CLIENTE) + auditoría.
/// </summary>
public interface IEmpresaService
{
    /// <summary>Registra un nuevo tenant, crea sus perfiles base y registra la auditoría.</summary>
    Task<EmpresaResponseDto> CreateAsync(EmpresaRequestDto dto);

    /// <summary>Obtiene una empresa por su Id.</summary>
    Task<EmpresaResponseDto?> GetByIdAsync(Guid id);

    /// <summary>Obtiene todas las empresas activas (panel Super Admin).</summary>
    Task<IEnumerable<EmpresaResponseDto>> GetAllAsync();

    /// <summary>Actualiza los datos de una empresa.</summary>
    Task<EmpresaResponseDto> UpdateAsync(Guid id, EmpresaRequestDto dto);

    /// <summary>Soft delete de una empresa. Solo desactiva; nunca elimina.</summary>
    Task<bool> DeactivateAsync(Guid id);
}