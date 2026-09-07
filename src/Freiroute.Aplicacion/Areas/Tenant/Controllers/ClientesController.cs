using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Cliente;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controlador MVC del catálogo de Clientes (HU-019).
/// Index es server-side (IClienteService.GetAllAsync) con paginación y
/// filtros; Create/Edit/Detalle delegan en el servicio BLL. El detalle
/// incluye contactos y ubicación de despacho por defecto (de HU-015).
/// </summary>
public class ClientesController : TenantBaseController
{
    private readonly IClienteService _clienteService;
    private readonly IUbicacionService _ubicacionService;

    public ClientesController(IClienteService clienteService, IUbicacionService ubicacionService)
    {
        _clienteService = clienteService;
        _ubicacionService = ubicacionService;
    }

    // ── GET: Index (lista paginada server-side) ─────────────────
    [HttpGet]
    public async Task<IActionResult> Index(string? tipoCliente, string? estadoCredito, string? q, int page = 1)
    {
        if (page < 1) page = 1;

        ViewData["ActiveMenu"] = "clientes";
        ViewData["FiltroTipo"] = tipoCliente;
        ViewData["FiltroEstado"] = estadoCredito;
        ViewData["Busqueda"] = q;

        var resultado = await _clienteService.GetAllAsync(EmpresaId, tipoCliente, estadoCredito, q, page, 20);
        return View(resultado);
    }

    // ── GET: Create ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "clientes";
        await CargarUbicacionesAsync();
        return View(new ClienteRequestDto());
    }

    // ── POST: Create ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClienteRequestDto dto)
    {
        ViewData["ActiveMenu"] = "clientes";

        if (!ModelState.IsValid)
        {
            await CargarUbicacionesAsync();
            return View(dto);
        }

        try
        {
            await _clienteService.CreateAsync(dto, EmpresaId);
            TempData["FrToast"] = "Cliente creado exitosamente";
            TempData["FrToastTipo"] = "success";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarUbicacionesAsync();
            return View(dto);
        }
    }

    // ── GET: Edit ───────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["ActiveMenu"] = "clientes";

        var cliente = await _clienteService.GetByIdAsync(id, EmpresaId);
        if (cliente is null)
        {
            return NotFound();
        }

        await CargarUbicacionesAsync();
        return View(MapToRequest(cliente));
    }

    // ── POST: Edit ──────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ClienteRequestDto dto)
    {
        ViewData["ActiveMenu"] = "clientes";

        if (!ModelState.IsValid)
        {
            await CargarUbicacionesAsync();
            return View(dto);
        }

        try
        {
            await _clienteService.UpdateAsync(id, dto, EmpresaId);
            TempData["FrToast"] = "Cliente actualizado exitosamente";
            TempData["FrToastTipo"] = "success";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await CargarUbicacionesAsync();
            return View(dto);
        }
    }

    // ── GET: Detalle ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id)
    {
        ViewData["ActiveMenu"] = "clientes";

        var cliente = await _clienteService.GetByIdAsync(id, EmpresaId);
        if (cliente is null)
        {
            return NotFound();
        }

        return View(cliente);
    }

    // ── Helpers ─────────────────────────────────────────────────
    private async Task CargarUbicacionesAsync()
    {
        var ubicaciones = await _ubicacionService.GetAllAsync(EmpresaId, null, null, 1, 500);
        ViewData["Ubicaciones"] = ubicaciones.Items
            .OrderBy(u => u.Nombre)
            .Select(u => new { u.Id, u.Nombre, u.Ciudad });
    }

    /// <summary>
    /// Mapea la respuesta (lectura) al DTO de formulario. Nota: el
    /// response DTO no expone TipoDocumento, SitioWeb ni Departamento —
    /// se mantienen los valores por defecto (RUC / vacío).
    /// </summary>
    private static ClienteRequestDto MapToRequest(ClienteResponseDto dto) => new()
    {
        Nombre = dto.Nombre,
        NombreComercial = dto.NombreComercial,
        RucNit = dto.RucNit,
        TipoCliente = dto.TipoCliente,
        Industria = dto.Industria,
        Email = dto.Email,
        Telefono = dto.Telefono,
        DireccionFiscal = dto.DireccionFiscal,
        Pais = dto.Pais,
        Ciudad = dto.Ciudad,
        UbicacionDefectoId = dto.UbicacionDefectoId,
        CreditoDias = dto.CreditoDias,
        LimiteCredito = dto.LimiteCredito,
        Moneda = dto.Moneda,
        SlaDiasEntrega = dto.SlaDiasEntrega,
        Contactos = dto.Contactos
            .Where(c => c.Activo)
            .Select(c => new ContactoClienteRequestDto
            {
                Nombre = c.Nombre,
                Cargo = c.Cargo,
                Rol = c.Rol,
                Email = c.Email,
                Telefono = c.Telefono,
                EsPrincipal = c.EsPrincipal
            })
            .ToList()
    };
}