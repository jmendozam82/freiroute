using Freiroute.BLL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Freiroute.API.Tests;

/// <summary>
/// WebApplicationFactory de pruebas de integración (tests/Freiroute.API.Tests).
/// Arranca la API real (Program.cs) pero sustituye los 6 servicios BLL por
/// mocks Moq, para poder testear los controllers sin tocar la base de datos.
///
/// NOTA: los repositorios DAL y stubs (Email/Supabase) se mantienen registrados,
/// pero al sustituir las interfaces IBLL los controllers no los usan.
/// Cada instancia del factory crea mocks frescos → aislamiento entre tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IAuthService> AuthService { get; }
    public Mock<IEmpresaService> EmpresaService { get; }
    public Mock<IPerfilService> PerfilService { get; }
    public Mock<IPermisoService> PermisoService { get; }
    public Mock<IUsuarioService> UsuarioService { get; }
    public Mock<IAuditoriaService> AuditoriaService { get; }

    // Servicios Sprint 2 (EP-02) — nuevos controllers Admin / Onboarding / Configuración.
    public Mock<IAdminDashboardService> AdminDashboardService { get; }
    public Mock<ISuscripcionService> SuscripcionService { get; }
    public Mock<IPlanService> PlanService { get; }
    public Mock<IPlanLimiteService> PlanLimiteService { get; }
    public Mock<IOnboardingService> OnboardingService { get; }
    public Mock<IConfiguracionService> ConfiguracionService { get; }

    // Servicios Sprint 3 — catálogos TMS (HU-015 a HU-020, G-06).
    public Mock<IUbicacionService> UbicacionService { get; }
    public Mock<IZonaEntregaService> ZonaEntregaService { get; }
    public Mock<ITarifaBaseService> TarifaBaseService { get; }
    public Mock<IClienteService> ClienteService { get; }
    public Mock<ITipoMercanciaService> TipoMercanciaService { get; }
    public Mock<IUnidadMedidaService> UnidadMedidaService { get; }
    public Mock<ITipoEmbalajeService> TipoEmbalajeService { get; }

    // Servicios Sprint 4 — Órdenes (HU-021 a HU-027).
    public Mock<IOrdenService> OrdenService { get; }
    public Mock<IOrdenImportService> OrdenImportService { get; }
    public Mock<IOrdenApiExternaService> OrdenApiExternaService { get; }
    public Mock<IPlantillaOrdenService> PlantillaOrdenService { get; }

    // Servicios Sprint 5 — PO/SO, prioridades, SLA, rechazos y reclamos (HU-028 a HU-032).
    public Mock<IOrdenPoService> OrdenPoService { get; }
    public Mock<IPrioridadOrdenService> PrioridadOrdenService { get; }
    public Mock<ISlaService> SlaService { get; }
    public Mock<IRechazoEntregaService> RechazoEntregaService { get; }
    public Mock<IReclamoService> ReclamoService { get; }

    public TestWebApplicationFactory()
    {
        AuthService = new Mock<IAuthService>();
        EmpresaService = new Mock<IEmpresaService>();
        PerfilService = new Mock<IPerfilService>();
        PermisoService = new Mock<IPermisoService>();
        UsuarioService = new Mock<IUsuarioService>();
        AuditoriaService = new Mock<IAuditoriaService>();

        AdminDashboardService = new Mock<IAdminDashboardService>();
        SuscripcionService = new Mock<ISuscripcionService>();
        PlanService = new Mock<IPlanService>();
        PlanLimiteService = new Mock<IPlanLimiteService>();
        OnboardingService = new Mock<IOnboardingService>();
        ConfiguracionService = new Mock<IConfiguracionService>();

        UbicacionService = new Mock<IUbicacionService>();
        ZonaEntregaService = new Mock<IZonaEntregaService>();
        TarifaBaseService = new Mock<ITarifaBaseService>();
        ClienteService = new Mock<IClienteService>();
        TipoMercanciaService = new Mock<ITipoMercanciaService>();
        UnidadMedidaService = new Mock<IUnidadMedidaService>();
        TipoEmbalajeService = new Mock<ITipoEmbalajeService>();

        OrdenService = new Mock<IOrdenService>();
        OrdenImportService = new Mock<IOrdenImportService>();
        OrdenApiExternaService = new Mock<IOrdenApiExternaService>();
        PlantillaOrdenService = new Mock<IPlantillaOrdenService>();

        OrdenPoService = new Mock<IOrdenPoService>();
        PrioridadOrdenService = new Mock<IPrioridadOrdenService>();
        SlaService = new Mock<ISlaService>();
        RechazoEntregaService = new Mock<IRechazoEntregaService>();
        ReclamoService = new Mock<IReclamoService>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, conf) =>
        {
            conf.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Security:TotpEncryptionKey", "TestTotpEncryptionKey12345678901234")
            });
        });

        builder.ConfigureServices(services =>
        {
            // Reemplazar las implementaciones reales por los mocks (los controllers
            // dependen de las interfaces IBLL, así que inyectamos mocks singleton).
            services.RemoveAll<IAuthService>();
            services.RemoveAll<IEmpresaService>();
            services.RemoveAll<IPerfilService>();
            services.RemoveAll<IPermisoService>();
            services.RemoveAll<IUsuarioService>();
            services.RemoveAll<IAuditoriaService>();
            services.RemoveAll<IAdminDashboardService>();
            services.RemoveAll<ISuscripcionService>();
            services.RemoveAll<IPlanService>();
            services.RemoveAll<IPlanLimiteService>();
            services.RemoveAll<IOnboardingService>();
            services.RemoveAll<IConfiguracionService>();
            services.RemoveAll<IUbicacionService>();
            services.RemoveAll<IZonaEntregaService>();
            services.RemoveAll<ITarifaBaseService>();
            services.RemoveAll<IClienteService>();
            services.RemoveAll<ITipoMercanciaService>();
            services.RemoveAll<IUnidadMedidaService>();
            services.RemoveAll<ITipoEmbalajeService>();
            
            services.RemoveAll<IOrdenService>();
            services.RemoveAll<IOrdenImportService>();
            services.RemoveAll<IOrdenApiExternaService>();
            services.RemoveAll<IPlantillaOrdenService>();
            services.RemoveAll<IOrdenPoService>();
            services.RemoveAll<IPrioridadOrdenService>();
            services.RemoveAll<ISlaService>();
            services.RemoveAll<IRechazoEntregaService>();
            services.RemoveAll<IReclamoService>();

            services.AddSingleton(AuthService.Object);
            services.AddSingleton(EmpresaService.Object);
            services.AddSingleton(PerfilService.Object);
            services.AddSingleton(PermisoService.Object);
            services.AddSingleton(UsuarioService.Object);
            services.AddSingleton(AuditoriaService.Object);
            services.AddSingleton(AdminDashboardService.Object);
            services.AddSingleton(SuscripcionService.Object);
            services.AddSingleton(PlanService.Object);
            services.AddSingleton(PlanLimiteService.Object);
            services.AddSingleton(OnboardingService.Object);
            services.AddSingleton(ConfiguracionService.Object);
            services.AddSingleton(UbicacionService.Object);
            services.AddSingleton(ZonaEntregaService.Object);
            services.AddSingleton(TarifaBaseService.Object);
            services.AddSingleton(ClienteService.Object);
            services.AddSingleton(TipoMercanciaService.Object);
            services.AddSingleton(UnidadMedidaService.Object);
            services.AddSingleton(TipoEmbalajeService.Object);

            services.AddScoped<IOrdenService>(_ =>
            {
                var mock = OrdenService;
                // Setup mínimo para GetAll → lista vacía
                mock.Setup(s => s.GetAllAsync(It.IsAny<Guid>(), It.IsAny<Freiroute.DTO.Orden.OrdenFiltroDto>()))
                    .ReturnsAsync(new Freiroute.Utility.Pagination.PagedResult<Freiroute.DTO.Orden.OrdenListDto>
                    {
                        Items = new System.Collections.Generic.List<Freiroute.DTO.Orden.OrdenListDto>(),
                        TotalItems = 0,
                        PageNumber = 1,
                        PageSize = 20
                    });
                // Setup para Create → 201
                mock.Setup(s => s.CreateAsync(
                    It.IsAny<Freiroute.DTO.Orden.OrdenRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
                    .ReturnsAsync(new Freiroute.DTO.Orden.OrdenResponseDto
                    {
                        Id = Guid.NewGuid(),
                        Estado = Freiroute.Utility.Constants.OrdenEstado.Draft
                    });
                return mock.Object;
            });

            services.AddScoped<IOrdenImportService>(_ => OrdenImportService.Object);
            services.AddScoped<IOrdenApiExternaService>(_ => OrdenApiExternaService.Object);
            services.AddScoped<IPlantillaOrdenService>(_ => PlantillaOrdenService.Object);
            services.AddScoped<IOrdenPoService>(_ => OrdenPoService.Object);
            services.AddScoped<IPrioridadOrdenService>(_ => PrioridadOrdenService.Object);
            services.AddScoped<ISlaService>(_ => SlaService.Object);
            services.AddScoped<IRechazoEntregaService>(_ => RechazoEntregaService.Object);
            services.AddScoped<IReclamoService>(_ => ReclamoService.Object);
        });
    }

    /// <summary>Crea un client HTTP anónimo (sin token).</summary>
    public HttpClient CrearClientSinToken() => CreateClient();

    /// <summary>Crea un client HTTP con el bearer token indicado.</summary>
    public HttpClient CrearClientConToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

