using System.Data;
using Dapper;
using Freiroute.Entity;

namespace Freiroute.DAL.Repositories;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly IDbConnection _db;

    public EmpresaRepository(IDbConnection db) => _db = db;

    // HU-001
    public async Task<Empresa?> GetByIdAsync(Guid id)
    {
        const string sql = @"SELECT * FROM empresas WHERE id = @Id AND activo = true";
        return await _db.QueryFirstOrDefaultAsync<Empresa>(sql, new { Id = id });
    }

    // HU-001: Validar unicidad del slug
    public async Task<Empresa?> GetBySlugAsync(string slug)
    {
        const string sql = @"SELECT * FROM empresas WHERE slug = @Slug AND activo = true";
        return await _db.QueryFirstOrDefaultAsync<Empresa>(sql, new { Slug = slug.ToLowerInvariant() });
    }

    // HU-001
    public async Task<Guid> CreateAsync(Empresa entity)
    {
        const string sql = @"
            INSERT INTO empresas (nombre, slug, plan, activo, fecha_creacion)
            VALUES (@Nombre, @Slug, @Plan, @Activo, NOW());
            SELECT gen_random_uuid();";
        
        var cmd = new CommandDefinition(sql, entity);
        return await _db.ExecuteScalarAsync<Guid>(cmd);
    }

    public async Task<IEnumerable<Empresa>> GetAllAsync()
    {
        const string sql = @"SELECT id, nombre, slug, plan, activo, fecha_creacion 
                             FROM empresas WHERE activo = true ORDER BY fecha_creacion DESC";
        return await _db.QueryAsync<Empresa>(sql);
    }
}
