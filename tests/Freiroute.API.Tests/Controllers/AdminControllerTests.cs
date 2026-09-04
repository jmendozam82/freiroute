using System.Net;
using System.Net.Http.Json;
using Freiroute.DTO.Admin;
using Freiroute.DTO.Auth;
using Freiroute.DTO.Plan;
using Freiroute.DTO.Suscripcion;
using Freiroute.Entity;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Exceptions;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del AdminController (panel global del SUPER_ADMIN,
/// HU-009/010/011). Los endpoints requieren SUPER_ADMIN (bypass de RequirePermission).
/// Planes y Suscripciones se testean vía BLL mocks; Empresas/Export/PagosHistory
/// requieren repositorios mock que el factory aún no provee.
/// </summary>
public class AdminControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public AdminControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose() => _factory.Dispose();

    /// <summary>Token de SUPER_ADMIN con permisos de configuración (read/create/update)
    /// para pasar RequirePermission antes de llegar a VerificarSuperAdmin.</summary>
    private static string SuperAdminToken => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), Guid.Empty,
        ["configuracion:read", "configuracion:create", "configuracion:update",
         "usuarios:read", "usuarios:create"],
        "SUPER_ADMIN");

    /// <summary>Token de ADMIN (no SUPER_ADMIN) con permisos de configuración,
    /// para verificar que VerificarSuperAdmin rechaza con 403.</summary>
    private static string AdminConPermisosToken => JwtTestHelper.GenerateTestToken(
        Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
        ["configuracion:read", "configuracion:create", "configuracion:update"],
        "ADMIN");

    private static LoginResponseDto LoginDto() => new()
    {
        AccessToken = "imp-token",
        ExpiresIn = 8 * 3600,
        Usuario = new UsuarioTokenDto
        {
            Id = Guid.NewGuid(),
            Nombre = "Admin",
            Email = "admin@tenant.com",
            TipoUsuario = "ADMIN",
            EmpresaNombre = "Trans SA",
            Permisos = ["embarques:read"]
        }
    };

    // ── Helpers: PlanResponseDto ─────────────────────────────────────

    private static PlanResponseDto PlanDto(Guid? id = null, string codigo = "STARTER") => new()
    {
        Id = id ?? Guid.NewGuid(),
        Nombre = "Plan Starter",
        Codigo = codigo,
        Descripcion = "Plan básico para startups",
        LimiteUsuarios = 5,
        LimiteEmbarquesMes = 100,
        LimiteStorageGb = 5,
        PrecioMensual = 49.99m,
        PrecioAnual = 499.99m,
        Moneda = "USD",
        ModulosDisponibles = ["embarques", "rastreo"],
        EsPublico = true,
        EmpresasSuscritas = 10,
        Activo = true,
        FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static PlanRequestDto PlanRequest() => new()
    {
        Nombre = "Plan Profesional",
        Codigo = "PROFESSIONAL",
        Descripcion = "Para empresas en crecimiento",
        LimiteUsuarios = 20,
        LimiteEmbarquesMes = 1000,
        LimiteStorageGb = 50,
        PrecioMensual = 149.99m,
        PrecioAnual = 1499.99m,
        Moneda = "USD",
        ModulosDisponibles = ["embarques", "rastreo", "facturacion"],
        EsPublico = true
    };

    // ── Helpers: SuscripcionResponseDto ──────────────────────────────

    private static SuscripcionResponseDto SuscripcionDto(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        EmpresaId = JwtTestHelper.EmpresaTenant,
        EmpresaNombre = "Trans SA",
        PlanId = Guid.NewGuid(),
        PlanNombre = "Plan Starter",
        PlanCodigo = "STARTER",
        TipoCiclo = "MENSUAL",
        FechaInicio = DateTime.UtcNow.AddDays(-30),
        FechaVencimiento = DateTime.UtcNow.AddDays(30),
        Estado = "ACTIVE",
        EstadoLabel = "Activa",
        PrecioPactado = 49.99m,
        MonedaPactada = "USD",
        DiasParaVencimiento = 30,
        Activo = true,
        FechaCreacion = DateTime.UtcNow.AddDays(-30)
    };

    private static PagoResponseDto PagoDto(Guid? suscripcionId = null) => new()
    {
        Id = Guid.NewGuid(),
        EmpresaId = JwtTestHelper.EmpresaTenant,
        EmpresaNombre = "Trans SA",
        SuscripcionId = suscripcionId ?? Guid.NewGuid(),
        Monto = 49.99m,
        Moneda = "USD",
        MetodoPago = "MANUAL",
        Referencia = "REF-001",
        Estado = "COMPLETED",
        PeriodoDesde = DateTime.UtcNow.AddDays(-30),
        PeriodoHasta = DateTime.UtcNow,
        RegistradoPorNombre = "Super Admin",
        FechaCreacion = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════
    //  EXISTING TESTS (sin cambios)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DashboardGlobal_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DashboardGlobal_SuperAdmin_Retorna200()
    {
        _factory.AdminDashboardService
            .Setup(s => s.GetDashboardGlobalAsync())
            .ReturnsAsync(new DashboardGlobalResponseDto { TotalEmpresasActivas = 5, Mrr = 1000m });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.GetAsync("/api/admin/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardFinanciero_SuperAdmin_Retorna200()
    {
        _factory.AdminDashboardService
            .Setup(s => s.GetDashboardFinancieroAsync())
            .ReturnsAsync(new DashboardFinancieroResponseDto { Mrr = 2000m, Arr = 24000m });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.GetAsync("/api/admin/dashboard/financiero");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Impersonar_EmpresaValida_Retorna200ConToken()
    {
        var empresaId = Guid.NewGuid();
        var login = LoginDto();
        _factory.AdminDashboardService
            .Setup(s => s.ImpersonarAsync(empresaId, It.IsAny<Guid>()))
            .ReturnsAsync(login);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PostAsync(
            $"/api/admin/empresas/{empresaId}/impersonar", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<ImpersonarResponseDto>>();
        body!.Data!.AccessToken.Should().Be("imp-token");
        body.Data.EmpresaNombre.Should().Be("Trans SA");
    }

    [Fact]
    public async Task CambiarPlan_SuperAdmin_Retorna200()
    {
        var empresaId = Guid.NewGuid();
        _factory.AdminDashboardService
            .Setup(s => s.CambiarPlanAsync(empresaId, It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/empresas/{empresaId}/plan",
            new CambiarPlanRequestDto { PlanId = Guid.NewGuid(), Motivo = "upgrade" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CambiarEstado_SuperAdmin_Retorna200()
    {
        var empresaId = Guid.NewGuid();
        _factory.AdminDashboardService
            .Setup(s => s.CambiarEstadoEmpresaAsync(empresaId, "SUSPENDED", It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSuperAdmin);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/empresas/{empresaId}/estado",
            new CambiarEstadoRequestDto { NuevoEstado = "SUSPENDED" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PLANES (HU-010) — GET all, GET by id, POST create, PUT update,
    //  DELETE deactivate + casos de error
    // ═══════════════════════════════════════════════════════════════════

    // ── GET /api/admin/planes ────────────────────────────────────────

    [Fact]
    public async Task GetAllPlanes_SuperAdmin_Retorna200ConLista()
    {
        var planes = new List<PlanResponseDto> { PlanDto(), PlanDto(Guid.NewGuid(), "PROFESSIONAL") };
        _factory.PlanService
            .Setup(s => s.GetAllAsync(false))
            .ReturnsAsync(planes);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync("/api/admin/planes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<PlanResponseDto>>>();
        body!.Success.Should().BeTrue();
        body.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllPlanes_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/admin/planes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/admin/planes/{id} ──────────────────────────────────

    [Fact]
    public async Task GetPlanById_PlanExistente_Retorna200()
    {
        var planId = Guid.NewGuid();
        _factory.PlanService
            .Setup(s => s.GetByIdAsync(planId))
            .ReturnsAsync(PlanDto(planId));

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync($"/api/admin/planes/{planId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<PlanResponseDto>>();
        body!.Data!.Id.Should().Be(planId);
        body.Data.Codigo.Should().Be("STARTER");
    }

    [Fact]
    public async Task GetPlanById_PlanNoExiste_Retorna404()
    {
        var planId = Guid.NewGuid();
        _factory.PlanService
            .Setup(s => s.GetByIdAsync(planId))
            .ReturnsAsync((PlanResponseDto?)null);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync($"/api/admin/planes/{planId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/admin/planes ──────────────────────────────────────

    [Fact]
    public async Task CreatePlan_DatosValidos_Retorna201Created()
    {
        var request = PlanRequest();
        var creado = PlanDto(codigo: "PROFESSIONAL");
        _factory.PlanService
            .Setup(s => s.CreateAsync(It.IsAny<PlanRequestDto>()))
            .ReturnsAsync(creado);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.PostAsJsonAsync("/api/admin/planes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<PlanResponseDto>>();
        body!.Data!.Codigo.Should().Be("PROFESSIONAL");
    }

    [Fact]
    public async Task CreatePlan_CodigoDuplicado_Retorna422()
    {
        var request = PlanRequest();
        _factory.PlanService
            .Setup(s => s.CreateAsync(It.IsAny<PlanRequestDto>()))
            .ThrowsAsync(new BusinessException("Ya existe un plan con el código PROFESSIONAL"));

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.PostAsJsonAsync("/api/admin/planes", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("PROFESSIONAL");
    }

    // ── PUT /api/admin/planes/{id} ─────────────────────────────────

    [Fact]
    public async Task UpdatePlan_DatosValidos_Retorna200()
    {
        var planId = Guid.NewGuid();
        var request = PlanRequest();
        var actualizado = PlanDto(planId, "PROFESSIONAL");
        _factory.PlanService
            .Setup(s => s.UpdateAsync(planId, It.IsAny<PlanRequestDto>()))
            .ReturnsAsync(actualizado);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.PutAsJsonAsync($"/api/admin/planes/{planId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<PlanResponseDto>>();
        body!.Data!.Id.Should().Be(planId);
    }

    [Fact]
    public async Task UpdatePlan_PlanNoExiste_Retorna404()
    {
        var planId = Guid.NewGuid();
        var request = PlanRequest();
        _factory.PlanService
            .Setup(s => s.UpdateAsync(planId, It.IsAny<PlanRequestDto>()))
            .ThrowsAsync(new NotFoundException(nameof(Freiroute.Entity.Plan), planId));

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.PutAsJsonAsync($"/api/admin/planes/{planId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/admin/planes/{id}/deactivate ────────────────────

    [Fact]
    public async Task DeactivatePlan_PlanActivo_Retorna200()
    {
        var planId = Guid.NewGuid();
        _factory.PlanService
            .Setup(s => s.DeactivateAsync(planId))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.DeleteAsync($"/api/admin/planes/{planId}/deactivate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        body!.Message.Should().Contain("desactivado");
    }

    [Fact]
    public async Task DeactivatePlan_ConEmpresasActivas_Retorna422()
    {
        var planId = Guid.NewGuid();
        _factory.PlanService
            .Setup(s => s.DeactivateAsync(planId))
            .ThrowsAsync(new BusinessException(
                "No se puede desactivar el plan porque tiene empresas activas suscritas"));

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.DeleteAsync($"/api/admin/planes/{planId}/deactivate");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("empresas activas");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SUSCRIPCIONES (HU-011) — GET all, GET by id, POST pago,
    //  GET pagos + casos de error
    // ═══════════════════════════════════════════════════════════════════

    // ── GET /api/admin/suscripciones ─────────────────────────────────

    [Fact]
    public async Task GetAllSuscripciones_SuperAdmin_Retorna200ConPaginacion()
    {
        var paged = new Freiroute.Utility.Pagination.PagedResult<SuscripcionResponseDto>
        {
            Items = new List<SuscripcionResponseDto> { SuscripcionDto() },
            TotalItems = 1,
            PageNumber = 1,
            PageSize = 20
        };
        _factory.SuscripcionService
            .Setup(s => s.GetAllAsync(null, 1, 20))
            .ReturnsAsync(paged);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync("/api/admin/suscripciones");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<Freiroute.Utility.Pagination.PagedResult<SuscripcionResponseDto>>>();
        body!.Data!.Items.Should().HaveCount(1);
        body.Data.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task GetAllSuscripciones_ConFiltroEstado_Retorna200()
    {
        var paged = new Freiroute.Utility.Pagination.PagedResult<SuscripcionResponseDto>
        {
            Items = new List<SuscripcionResponseDto>(),
            TotalItems = 0,
            PageNumber = 1,
            PageSize = 20
        };
        _factory.SuscripcionService
            .Setup(s => s.GetAllAsync("SUSPENDED", 1, 20))
            .ReturnsAsync(paged);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync("/api/admin/suscripciones?estado=SUSPENDED");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<Freiroute.Utility.Pagination.PagedResult<SuscripcionResponseDto>>>();
        body!.Data!.Items.Should().BeEmpty();
    }

    // ── GET /api/admin/suscripciones/{id} ────────────────────────────

    [Fact]
    public async Task GetSuscripcion_Existe_Retorna200()
    {
        var susId = Guid.NewGuid();
        _factory.SuscripcionService
            .Setup(s => s.GetByIdAsync(susId))
            .ReturnsAsync(SuscripcionDto(susId));

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync($"/api/admin/suscripciones/{susId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<SuscripcionResponseDto>>();
        body!.Data!.Id.Should().Be(susId);
        body.Data.Estado.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetSuscripcion_NoExiste_Retorna404()
    {
        var susId = Guid.NewGuid();
        _factory.SuscripcionService
            .Setup(s => s.GetByIdAsync(susId))
            .ReturnsAsync((SuscripcionResponseDto?)null);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync($"/api/admin/suscripciones/{susId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/admin/suscripciones/{id}/pago ─────────────────────

    [Fact]
    public async Task RegistrarPago_DatosValidos_Retorna201Created()
    {
        var susId = Guid.NewGuid();
        var pagoRequest = new PagoRequestDto
        {
            SuscripcionId = susId,
            Monto = 49.99m,
            Moneda = "USD",
            MetodoPago = "MANUAL",
            PeriodoDesde = DateTime.UtcNow.AddDays(-30),
            PeriodoHasta = DateTime.UtcNow
        };
        var pagoResponse = PagoDto(susId);
        _factory.SuscripcionService
            .Setup(s => s.RegistrarPagoAsync(susId, It.IsAny<PagoRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(pagoResponse);

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.PostAsJsonAsync($"/api/admin/suscripciones/{susId}/pago", pagoRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<PagoResponseDto>>();
        body!.Data!.SuscripcionId.Should().Be(susId);
        body.Data.Estado.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task RegistrarPago_SuscripcionNoExiste_Retorna404()
    {
        var susId = Guid.NewGuid();
        var pagoRequest = new PagoRequestDto
        {
            SuscripcionId = susId,
            Monto = 49.99m,
            PeriodoDesde = DateTime.UtcNow.AddDays(-30),
            PeriodoHasta = DateTime.UtcNow
        };
        _factory.SuscripcionService
            .Setup(s => s.RegistrarPagoAsync(susId, It.IsAny<PagoRequestDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new NotFoundException(nameof(Freiroute.Entity.Suscripcion), susId));

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.PostAsJsonAsync($"/api/admin/suscripciones/{susId}/pago", pagoRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegistrarPago_MontoInvalido_Retorna422()
    {
        var susId = Guid.NewGuid();
        var pagoRequest = new PagoRequestDto
        {
            SuscripcionId = susId,
            Monto = -10m,
            PeriodoDesde = DateTime.UtcNow.AddDays(-30),
            PeriodoHasta = DateTime.UtcNow
        };
        _factory.SuscripcionService
            .Setup(s => s.RegistrarPagoAsync(susId, It.IsAny<PagoRequestDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new BusinessException("El monto del pago debe ser mayor a cero"));

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.PostAsJsonAsync($"/api/admin/suscripciones/{susId}/pago", pagoRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("monto");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DASHBOARD FINANCIERO (HU-011)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DashboardFinanciero_ConDatos_Retorna200ConMrrArr()
    {
        _factory.AdminDashboardService
            .Setup(s => s.GetDashboardFinancieroAsync())
            .ReturnsAsync(new DashboardFinancieroResponseDto
            {
                Mrr = 5420.50m,
                Arr = 65046.00m
            });

        var client = _factory.CrearClientConToken(SuperAdminToken);

        var response = await client.GetAsync("/api/admin/dashboard/financiero");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiDto<DashboardFinancieroResponseDto>>();
        body!.Data!.Mrr.Should().Be(5420.50m);
        body.Data.Arr.Should().Be(65046.00m);
    }

    [Fact]
    public async Task DashboardFinanciero_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/admin/dashboard/financiero");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NO-SUPER_ADMIN → 403 (regla de VerificarSuperAdmin)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllPlanes_AdminNoSuperAdmin_Retorna403()
    {
        var client = _factory.CrearClientConToken(AdminConPermisosToken);

        var response = await client.GetAsync("/api/admin/planes");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllEmpresas_AdminNoSuperAdmin_Retorna403()
    {
        var client = _factory.CrearClientConToken(AdminConPermisosToken);

        var response = await client.GetAsync("/api/admin/empresas");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllSuscripciones_AdminNoSuperAdmin_Retorna403()
    {
        var client = _factory.CrearClientConToken(AdminConPermisosToken);

        var response = await client.GetAsync("/api/admin/suscripciones");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegistrarPago_AdminNoSuperAdmin_Retorna403()
    {
        var client = _factory.CrearClientConToken(AdminConPermisosToken);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/suscripciones/{Guid.NewGuid()}/pago",
            new PagoRequestDto
            {
                SuscripcionId = Guid.NewGuid(),
                Monto = 10m,
                PeriodoDesde = DateTime.UtcNow.AddDays(-1),
                PeriodoHasta = DateTime.UtcNow
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DashboardFinanciero_AdminNoSuperAdmin_Retorna403()
    {
        var client = _factory.CrearClientConToken(AdminConPermisosToken);

        var response = await client.GetAsync("/api/admin/dashboard/financiero");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePlan_AdminNoSuperAdmin_Retorna403()
    {
        var client = _factory.CrearClientConToken(AdminConPermisosToken);

        var response = await client.PostAsJsonAsync("/api/admin/planes", PlanRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Helper
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Helper que refleja el wrapper ApiResponse&lt;T&gt; solo para deserializar los tests.</summary>
    private record ApiDto<T>(T? Data);
}
