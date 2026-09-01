using Freiroute.BLL.Services;
using Freiroute.DAL.Repositories;
using Freiroute.DTO.Empresa;
using FluentValidation;

namespace Freiroute.BLL.Services;

// HU-001
public class EmpresaService : IEmpresaService
{
    private readonly IEmpresaRepository _repo;

    public EmpresaService(IEmpresaRepository repo) => _repo = repo;

    public async Task<EmpresaResponseDto> CrearAsync(EmpresaRequestDto dto)
    {
        var validator = new EmpresaValidator();
        var validationResult = await validator.ValidateAsync(dto);
        
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Derivación automática del Slug si se omite
        var slug = string.IsNullOrWhiteSpace(dto.Slug) 
            ? generarSlug(dto.Nombre) 
            : dto.Slug.ToLowerInvariant();

        // Verificar unicidad global del Slug
        var existente = await _repo.GetBySlugAsync(slug);
        if (existente != null)
            throw new InvalidOperationException($"El slug '{slug}' ya está en uso.");

        var empresa = new Entity.Empresa
        {
            Nombre = dto.Nombre.Trim(),
            Slug = slug,
            Plan = dto.Plan,
            Activo = true
        };

        empresa.Id = await _repo.CreateAsync(empresa);
        
        return new EmpresaResponseDto
        {
            Id = empresa.Id,
            Nombre = empresa.Nombre,
            Slug = empresa.Slug,
            Plan = empresa.Plan,
            Activo = empresa.Activo,
            FechaCreacion = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<EmpresaResponseDto>> GetAllAsync()
    {
        var empresas = await _repo.GetAllAsync();
        return empresas.Select(e => new EmpresaResponseDto
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Slug = e.Slug,
            Plan = e.Plan,
            Activo = e.Activo,
            FechaCreacion = e.FechaCreacion
        });
    }

    private static string generarSlug(string nombre) => 
        new string(nombre.Where(char.IsLetterOrDigit).ToArray())
                      .Replace(" ", "-").ToLowerInvariant();
}
