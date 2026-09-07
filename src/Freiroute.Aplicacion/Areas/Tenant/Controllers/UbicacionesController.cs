using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Ubicacion;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controlador MVC del catálogo de Ubicaciones (HU-015).
/// Index es server-side (IUbicacionService.GetAllAsync) para paginación
/// y filtros sin dependencia del JWT del cliente; Create/Edit delegan
/// en el servicio BLL que dispara la geocodificación automática (ADR-014).
/// </summary>
public class UbicacionesController : TenantBaseController
{
    private readonly IUbicacionService _ubicacionService;

    public UbicacionesController(IUbicacionService ubicacionService)
    {
        _ubicacionService = ubicacionService;
    }

    // ── GET: Index (lista paginada server-side) ────────────────
    [HttpGet]
    public async Task<IActionResult> Index(string? tipo, string? q, int page = 1)
    {
        if (page < 1) page = 1;

        ViewData["ActiveMenu"] = "ubicaciones";
        ViewData["FiltroTipo"] = tipo;
        ViewData["Busqueda"] = q;

        var resultado = await _ubicacionService.GetAllAsync(EmpresaId, tipo, q, page, 20);
        return View(resultado);
    }

    // ── GET: Create ─────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["ActiveMenu"] = "ubicaciones";
        return View(new UbicacionRequestDto());
    }

    // ── POST: Create ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UbicacionRequestDto dto)
    {
        ViewData["ActiveMenu"] = "ubicaciones";

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            var creada = await _ubicacionService.CreateAsync(dto, EmpresaId);

            // CA-03: se guardó sin coordenadas si la geocodificación falló.
            if (!creada.Georeferenciada && !string.IsNullOrWhiteSpace(dto.Direccion))
            {
                TempData["FrToast"] = "Ubicación creada. No se pudo geocodificar la dirección; puede ajustar el pin en el mapa.";
                TempData["FrToastTipo"] = "warning";
            }
            else
            {
                TempData["FrToast"] = "Ubicación creada exitosamente";
                TempData["FrToastTipo"] = "success";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    // ── GET: Edit ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["ActiveMenu"] = "ubicaciones";

        var ubicacion = await _ubicacionService.GetByIdAsync(id, EmpresaId);
        if (ubicacion is null)
        {
            return NotFound();
        }

        return View(MapToRequest(ubicacion));
    }

    // ── POST: Edit ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UbicacionRequestDto dto)
    {
        ViewData["ActiveMenu"] = "ubicaciones";

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            await _ubicacionService.UpdateAsync(id, dto, EmpresaId);
            TempData["FrToast"] = "Ubicación actualizada exitosamente";
            TempData["FrToastTipo"] = "success";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }

    /// <summary>
    /// Mapea la respuesta (lectura) al DTO de formulario. Nota: el
    /// response DTO no expone CodigoPostal, ContactoEmail, horarios ni
    /// instrucciones — el usuario los reintroduce al editar (campos opcionales).
    /// </summary>
    private static UbicacionRequestDto MapToRequest(UbicacionResponseDto dto) => new()
    {
        Nombre = dto.Nombre,
        Codigo = dto.Codigo,
        Tipo = dto.Tipo,
        Direccion = dto.Direccion,
        Pais = dto.Pais,
        Departamento = dto.Departamento,
        Ciudad = dto.Ciudad,
        ContactoNombre = dto.ContactoNombre,
        ContactoTelefono = dto.ContactoTelefono,
        TiempoServicioMin = dto.TiempoServicioMin,
        Latitud = dto.Latitud,
        Longitud = dto.Longitud
    };
}