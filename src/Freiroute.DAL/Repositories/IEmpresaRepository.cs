using Freiroute.Entity;

namespace Freiroute.DAL.Repositories;

public interface IEmpresaRepository
{
    Task<Empresa?> GetByIdAsync(Guid id);
    Task<Empresa?> GetBySlugAsync(string slug);
    Task<Guid> CreateAsync(Empresa entity);
    Task<IEnumerable<Empresa>> GetAllAsync();
}
