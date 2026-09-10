using Freiroute.Aplicacion.Areas.Tenant.Controllers;
using Freiroute.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.Aplicacion.Areas.Tenant.Controllers;

public class ConfiguracionController : TenantBaseController
{
    private readonly IConfiguracionService _configuracionService;

    public ConfiguracionController(IConfiguracionService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var config = await _configuracionService.GetAsync(EmpresaId);
        return View(config);
    }
}
