using Freiroute.DTO.Empresa;
using Freiroute.Entity;

namespace Freiroute.BLL.Services;

// HU-001
public interface IEmpresaService
{
    Task<EmpresaResponseDto> CrearAsync(EmpresaRequestDto dto);
    Task<IEnumerable<EmpresaResponseDto>> GetAllAsync();
}
