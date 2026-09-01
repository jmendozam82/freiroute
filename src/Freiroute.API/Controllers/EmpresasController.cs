using Freiroute.BLL.Services;
using Freiroute.DTO.Empresa;
using Freiroute.Entity;
using Freiroute.Utility.ApiResponse;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Freiroute.API.Controllers;

/// <summary>
/// Controlador REST para gestión de empresas (tenants) del SaaS Freiroute TMS.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _service;

    public EmpresasController(IEmpresaService service) => _service = service;

    /// <summary>
    /// Crea una nueva empresa (tenant) en el sistema. Solo SuperAdmin puede ejecutar.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SuperAdminPolicy")] 
    [ProducesResponseType(typeof(ApiResponse<EmpresaResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] EmpresaRequestDto dto)
    {
        try
        {
            var result = await _service.CrearAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<EmpresaResponseDto>.Ok(result));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResponse<EmpresaResponseDto>.Error(ex.Message, ex.Errors.Select(e => e.ErrorMessage).ToList()));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<EmpresaResponseDto>.Error(ex.Message));
        }
    }

    /// <summary>
    /// Obtiene una empresa por su ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EmpresaResponseDto>), StatusCodes.Status200OK)]
    public IActionResult GetById(Guid id)
    {
        // Implementación pendiente de DAL completa + Service
        return Ok(ApiResponse<EmpresaResponseDto>.Ok(new EmpresaResponseDto())); 
    }
}
