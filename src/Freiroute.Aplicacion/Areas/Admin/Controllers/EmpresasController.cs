using Freiroute.BLL.Services;
using Freiroute.DTO.Empresa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Freiroute.Aplicacion.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "SuperAdminPolicy")]
public class EmpresasController : Controller
{
    private readonly IEmpresaService _service;
    private readonly ILogger<EmpresasController> _logger;

    public EmpresasController(IEmpresaService service, ILogger<EmpresasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var empresas = await _service.GetAllAsync();
            return View(empresas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener lista de empresas");
            TempData["ErrorMessage"] = "Error al cargar la lista de tenants. Intente nuevamente.";
            return View(new List<EmpresaResponseDto>());
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.PlanOptions = new SelectList(new[]
        {
            new { Value = "starter", Text = "Starter" },
            new { Value = "professional", Text = "Professional" },
            new { Value = "enterprise", Text = "Enterprise" }
        }, "Value", "Text", "starter");

        return View(new EmpresaRequestDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmpresaRequestDto dto)
    {
        ViewBag.PlanOptions = new SelectList(new[]
        {
            new { Value = "starter", Text = "Starter" },
            new { Value = "professional", Text = "Professional" },
            new { Value = "enterprise", Text = "Enterprise" }
        }, "Value", "Text", dto.Plan);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Validación fallida al crear tenant: {@ModelErrors}", ModelState.Values.SelectMany(v => v.Errors));
            return View(dto);
        }

        try
        {
            await _service.CrearAsync(dto);
            TempData["SuccessMessage"] = "Tenant creado exitosamente";
            Log.Information("Tenant creado: {@Slug}", dto.Slug);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogWarning(ex, "Conflict al crear tenant con slug duplicado: {@Slug}", dto.Slug);
            return View(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear tenant");
            TempData["ErrorMessage"] = "Ocurrió un error al crear el tenant. Intente nuevamente.";
            return View(dto);
        }
    }
}
