using Freiroute.Aplicacion.Areas.Tenant.ViewModels;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Orden;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

/// <summary>
/// Controller MVC del módulo de órdenes de transporte (HU-021, HU-024,
/// HU-026, HU-027). Sigue el patrón server-side del área Tenant
/// (ClientesController): inyecta los servicios BLL y renderiza
/// PagedResult / ResponseDto en las vistas. El cambio de estado, el
/// historial y el soft delete se ejecutan desde el detalle via FrApi
/// (PATCH /estado, GET /historial, DELETE /deactivate).
/// Los permisos de módulo se verifican a nivel API ([RequirePermission])
/// y en la UI con User.HasPermission — los controllers MVC del área no
/// usan [RequireModulePermission] (patrón existente del proyecto).
/// </summary>
public class OrdenesController : TenantBaseController
{
    /// <summary>20 registros por página — RNF-01.4.</summary>
    private const int PageSize = 20;

    private readonly IOrdenService _ordenService;
    private readonly IClienteService _clienteService;
    private readonly IUbicacionService _ubicacionService;
    private readonly ITipoMercanciaService _tipoMercanciaService;
    private readonly IUnidadMedidaService _unidadMedidaService;
    private readonly ITipoEmbalajeService _tipoEmbalajeService;
    private readonly ITarifaBaseService _tarifaService;

    public OrdenesController(
        IOrdenService ordenService,
        IClienteService clienteService,
        IUbicacionService ubicacionService,
        ITipoMercanciaService tipoMercanciaService,
        IUnidadMedidaService unidadMedidaService,
        ITipoEmbalajeService tipoEmbalajeService,
        ITarifaBaseService tarifaService)
    {
        _ordenService = ordenService;
        _clienteService = clienteService;
        _ubicacionService = ubicacionService;
        _tipoMercanciaService = tipoMercanciaService;
        _unidadMedidaService = unidadMedidaService;
        _tipoEmbalajeService = tipoEmbalajeService;
        _tarifaService = tarifaService;
    }

    // ── GET: Índice ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index(
        string? estado, string? prioridad, string? busqueda, string? po, int page = 1)
    {
        ViewData["ActiveMenu"] = "ordenes";
        ViewData["Title"] = "Órdenes de Transporte";

        var filtro = new OrdenFiltroDto
        {
            Q = busqueda,
            Estado = estado,
            Prioridad = prioridad,
            Po = po,
            Page = page,
            PageSize = PageSize,
        };

        var resultado = await _ordenService.GetAllAsync(EmpresaId, filtro);

        var vm = new OrdenIndexViewModel
        {
            Resultados = resultado,
            Estado = estado,
            Prioridad = prioridad,
            Busqueda = busqueda,
            Po = po,
        };

        return View(vm);
    }

    // ── GET: Órdenes en riesgo SLA (HU-031 CA-02) ────────────
    /// <summary>
    /// Vista de órdenes con entrega en las próximas 24 horas sin confirmar
    /// tránsito. Los datos se cargan desde el cliente vía FrApi hacia
    /// GET /api/ordenes/sla-en-riesgo (la API ya está verificada en Fase 4).
    /// </summary>
    [HttpGet]
    public IActionResult SlaEnRiesgo()
    {
        ViewData["ActiveMenu"] = "ordenes";
        ViewData["Title"] = "Órdenes en Riesgo SLA";
        return View();
    }

    // ── GET: Órdenes críticas (HU-029 CA-04/CA-05) ────────────
    /// <summary>
    /// Vista de órdenes CRITICO/ALTO sin avance significativo. Los datos se
    /// cargan desde el cliente vía FrApi hacia GET /api/ordenes/criticas.
    /// </summary>
    [HttpGet]
    public IActionResult Criticas()
    {
        ViewData["ActiveMenu"] = "ordenes";
        ViewData["Title"] = "Órdenes Críticas";
        return View();
    }

    // ── GET: Crear ──────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["ActiveMenu"] = "ordenes";
        ViewData["Title"] = "Nueva orden";

        var vm = new OrdenFormViewModel();
        await CargarMaestrosAsync(vm);
        return View(vm);
    }

    // ── POST: Crear ─────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrdenFormViewModel vm)
    {
        ViewData["ActiveMenu"] = "ordenes";
        ViewData["Title"] = "Nueva orden";

        if (!ModelState.IsValid)
        {
            await CargarMaestrosAsync(vm);
            return View(vm);
        }

        try
        {
            var creada = await _ordenService.CreateAsync(vm.ToRequest(), EmpresaId, UsuarioId);
            TempData["FrToast"] = $"Orden {creada.NumeroOrden ?? "borrador"} creada correctamente";
            TempData["FrToastTipo"] = "success";
            return RedirectToAction(nameof(Detalle), new { id = creada.Id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
        }

        await CargarMaestrosAsync(vm);
        return View(vm);
    }

    // ── GET: Detalle ────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Detalle(Guid id)
    {
        ViewData["ActiveMenu"] = "ordenes";

        var orden = await _ordenService.GetByIdAsync(id, EmpresaId);
        if (orden is null)
        {
            return NotFound();
        }

        ViewData["Title"] = orden.NumeroOrden ?? "Borrador";
        return View(orden);
    }

    // ── GET: Editar ─────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        ViewData["ActiveMenu"] = "ordenes";

        var orden = await _ordenService.GetByIdAsync(id, EmpresaId);
        if (orden is null)
        {
            return NotFound();
        }

        var vm = await ToFormAsync(orden);
        ViewData["EstadoActual"] = orden.Estado;
        ViewData["NumeroOrden"] = orden.NumeroOrden;
        ViewData["OrdenId"] = orden.Id;
        ViewData["Title"] = $"Editar {orden.NumeroOrden ?? "borrador"}";
        return View(vm);
    }

    // ── POST: Editar ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, OrdenFormViewModel vm)
    {
        ViewData["ActiveMenu"] = "ordenes";

        var orden = await _ordenService.GetByIdAsync(id, EmpresaId);
        if (orden is null)
        {
            return NotFound();
        }

        ViewData["EstadoActual"] = orden.Estado;
        ViewData["NumeroOrden"] = orden.NumeroOrden;
        ViewData["OrdenId"] = orden.Id;
        ViewData["Title"] = $"Editar {orden.NumeroOrden ?? "borrador"}";

        // Solo DRAFT o CONFIRMED son editables (HU-021 CA-12, OrdenService)
        if (!OrdenEstado.Editables.Contains(orden.Estado))
        {
            TempData["FrToast"] = "La orden solo puede editarse en estado Borrador o Confirmada";
            TempData["FrToastTipo"] = "warning";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // En CONFIRMED la ruta queda bloqueada (HU-026): restaurar desde
        // la BD los selects deshabilitados — nunca confiar en el form.
        if (orden.Estado == OrdenEstado.Confirmed)
        {
            vm.ClienteId = orden.ClienteId;
            vm.OrigenId = orden.OrigenId;
            vm.DestinoId = orden.DestinoId;
        }

        if (!ModelState.IsValid)
        {
            await CargarMaestrosAsync(vm);
            return View(vm);
        }

        try
        {
            var actualizada = await _ordenService.UpdateAsync(id, vm.ToRequest(), EmpresaId, UsuarioId);
            TempData["FrToast"] = $"Orden {actualizada.NumeroOrden ?? "borrador"} actualizada correctamente";
            TempData["FrToastTipo"] = "success";
            return RedirectToAction(nameof(Detalle), new { id });
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (FluentValidation.ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
        }

        await CargarMaestrosAsync(vm);
        return View(vm);
    }

    // ── Helpers ─────────────────────────────────────────────────
    /// <summary>
    /// Carga los 6 catálogos para los selects del formulario (CA-17).
    /// Los catálogos no paginan (TipoMercancia/Unidad/Embalaje) se traen
    /// completos; los paginados (Cliente/Ubicacion/Tarifa) con un tamaño
    /// alto para poblar el select sin buscador.
    /// </summary>
    private async Task CargarMaestrosAsync(OrdenFormViewModel vm)
    {
        var clientes = await _clienteService.GetAllAsync(EmpresaId, null, null, null, 1, 1000);
        vm.Clientes = clientes.Items
            .OrderBy(c => c.Nombre)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre })
            .ToList();

        var ubicaciones = await _ubicacionService.GetAllAsync(EmpresaId, null, null, 1, 500);
        vm.Ubicaciones = ubicaciones.Items
            .OrderBy(u => u.Nombre)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = string.IsNullOrEmpty(u.Ciudad) ? u.Nombre : $"{u.Nombre} ({u.Ciudad})",
            })
            .ToList();

        var tiposMercancia = await _tipoMercanciaService.GetAllAsync(EmpresaId);
        vm.TiposMercancia = tiposMercancia
            .OrderBy(t => t.Nombre)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Nombre })
            .ToList();

        var unidades = await _unidadMedidaService.GetAllAsync(EmpresaId);
        vm.Unidades = unidades
            .OrderBy(u => u.Nombre)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = string.IsNullOrEmpty(u.Simbolo) ? u.Nombre : $"{u.Nombre} ({u.Simbolo})",
            })
            .ToList();

        var embalajes = await _tipoEmbalajeService.GetAllAsync(EmpresaId);
        vm.Embalajes = embalajes
            .OrderBy(e => e.Nombre)
            .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Nombre })
            .ToList();

        var tarifas = await _tarifaService.GetAllAsync(EmpresaId, null, null, null, false, 1, 200);
        vm.Tarifas = tarifas.Items
            .OrderBy(t => t.Nombre)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Nombre })
            .ToList();
    }

    /// <summary>Mapea la respuesta de lectura al ViewModel y carga los catálogos.</summary>
    private async Task<OrdenFormViewModel> ToFormAsync(OrdenResponseDto orden)
    {
        var vm = new OrdenFormViewModel
        {
            ClienteId = orden.ClienteId,
            OrigenId = orden.OrigenId,
            DestinoId = orden.DestinoId,
            TipoMercanciaId = orden.TipoMercanciaId,
            UnidadMedidaId = orden.UnidadMedidaId,
            TipoEmbalajeId = orden.TipoEmbalajeId,
            TarifaId = orden.TarifaId,
            Cantidad = orden.Cantidad,
            PesoKg = orden.PesoKg,
            VolumenM3 = orden.VolumenM3,
            ValorDeclarado = orden.ValorDeclarado,
            ModoTransporte = orden.ModoTransporte,
            NivelServicio = orden.NivelServicio,
            Prioridad = orden.Prioridad,
            FechaPickupSolicitada = orden.FechaPickupSolicitada,
            FechaEntregaRequerida = orden.FechaEntregaRequerida,
            ReferenciaCliente = orden.ReferenciaCliente,
            Instrucciones = orden.Instrucciones,
            NumeroPo = orden.NumeroPo,
            NumeroSo = orden.NumeroSo,
        };
        await CargarMaestrosAsync(vm);
        return vm;
    }
}