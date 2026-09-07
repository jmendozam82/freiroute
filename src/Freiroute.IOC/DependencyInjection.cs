using System.Data;
using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Services;
using Freiroute.BLL.Settings;
using Freiroute.DAL.Interfaces;
using Freiroute.DAL.Repositories;
using Freiroute.DTO.Auth;
using Freiroute.DTO.Empresa;
using Freiroute.DTO.Permiso;
using Freiroute.DTO.Perfil;
using Freiroute.DTO.Usuario;
using Freiroute.DTO.Plan;
using Freiroute.DTO.Suscripcion;
using Freiroute.DTO.Onboarding;
using Freiroute.DTO.Configuracion;
using Freiroute.DTO.Admin;
using Freiroute.DTO.Ubicacion;
using Freiroute.DTO.Zona;
using Freiroute.DTO.Mercancia;
using Freiroute.DTO.Unidad;
using Freiroute.DTO.Cliente;
using Freiroute.DTO.Tarifa;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Freiroute.IOC;

/// <summary>
/// Composition root del sistema Freiroute TMS (ADR-002).
/// Centraliza el registro de dependencias DI para todas las capas:
///  DB Connection → DAL Repositories → BLL Services → FluentValidation Validators.
/// Cada módulo nuevo se registra siguiendo esta estructura.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFreirouteServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── 1. Configuración tipada (appsettings) ──────────────────
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<AppSettings>(configuration.GetSection("App"));

        // ── 2. Conexión DB — IDbConnection scoped con Npgsql ───────
        //    Scoped: cada request recibe su propia conexión (una conexión Npgsql
        //    compartida como Singleton no es thread-safe para requests concurrentes).
        services.AddScoped<IDbConnection>(sp =>
            new NpgsqlConnection(configuration.GetConnectionString("SupabaseConnection")));

        // ── 3. HttpContextAccessor (IP/User-Agent para auditoría auth) ──
        services.AddHttpContextAccessor();

        // ── 4. Repositorios DAL ─────────────────────────────────────
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IPermisoRepository, PermisoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<IInvitacionRepository, InvitacionRepository>();
        services.AddScoped<ISesionRepository, SesionRepository>();

        // ── Repositorios Sprint 2 (EP-02 Admin SaaS & Tenants) ──────
        services.AddScoped<IPlanRepository,             PlanRepository>();
        services.AddScoped<ISuscripcionRepository,      SuscripcionRepository>();
        services.AddScoped<IPagoRepository,             PagoRepository>();
        services.AddScoped<IConfiguracion2faRepository, Configuracion2faRepository>();
        services.AddScoped<IConfiguracionRepository,    ConfiguracionRepository>();

        // ── Repositorios Sprint 3 (EP-03 Maestros y Catálogos) ─────
        services.AddScoped<IUbicacionRepository,      UbicacionRepository>();
        services.AddScoped<IZonaEntregaRepository,    ZonaEntregaRepository>();
        services.AddScoped<ITipoMercanciaRepository,  TipoMercanciaRepository>();
        services.AddScoped<IUnidadMedidaRepository,   UnidadMedidaRepository>();
        services.AddScoped<ITipoEmbalajeRepository,   TipoEmbalajeRepository>();
        services.AddScoped<IClienteRepository,        ClienteRepository>();
        services.AddScoped<ITarifaBaseRepository,     TarifaBaseRepository>();

        // ── 5. Infraestructura BLL ──────────────────────────────────
        services.AddSingleton<IJwtService, JwtService>();
        services.AddHttpClient<IEmailService, ResendEmailService>();
        // Supabase Auth: real en producción/CI con credenciales, stub en dev sin ellas.
        var usarAuthReal = configuration.GetValue<bool?>("Supabase:UseRealAuth") ?? false;
        if (usarAuthReal)
        {
            services.AddScoped<ISupabaseAuthService, SupabaseAuthServiceReal>();
        }
        else
        {
            services.AddScoped<ISupabaseAuthService, SupabaseAuthServiceStub>();
        }
        // Transversal: la auditoría se inyecta en todos los demás servicios.
        services.AddScoped<IAuditoriaService, AuditoriaService>();

        // ── 6. Servicios BLL ────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IPermisoService, PermisoService>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        // ── 6b. Servicios BLL Sprint 2 (EP-02 Admin SaaS & Tenants) ──
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IPlanLimiteService, PlanLimiteService>();
        services.AddScoped<ISuscripcionService, SuscripcionService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IConfiguracionService, ConfiguracionService>();

        // ── 6c. Supabase Storage (HU-014, ADR-012) via HttpClient ───
        services.AddHttpClient<IStorageService, SupabaseStorageService>();

        // ── 6d. Servicios BLL Sprint 3 (EP-03 Maestros y Catálogos) ──
        services.AddScoped<IUbicacionService, UbicacionService>();
        services.AddScoped<IZonaEntregaService, ZonaEntregaService>();
        services.AddScoped<ITipoMercanciaService, TipoMercanciaService>();
        services.AddScoped<IUnidadMedidaService, UnidadMedidaService>();
        services.AddScoped<ITipoEmbalajeService, TipoEmbalajeService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<ITarifaBaseService, TarifaBaseService>();

        // ── 6e. Geocodificación Nominatim (ADR-014) via HttpClient ───
        //    User-Agent identificable y timeout 10s (política de uso de OSM);
        //    el servicio serializa internamente 1 request/segundo.
        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(
            client =>
            {
                client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "FreirouteTMS/1.0 (dev@freiroute.com)");
            });

        // ── 7. Validadores FluentValidation (validación servidor) ──
        services.AddScoped<IValidator<LoginRequestDto>, BLL.Validators.LoginValidator>();
        services.AddScoped<IValidator<EmpresaRequestDto>, BLL.Validators.EmpresaValidator>();
        services.AddScoped<IValidator<PerfilRequestDto>, BLL.Validators.PerfilValidator>();
        services.AddScoped<IValidator<UsuarioRequestDto>, BLL.Validators.UsuarioValidator>();
        services.AddScoped<IValidator<ResetPasswordRequestDto>, BLL.Validators.ResetPasswordValidator>();
        services.AddScoped<IValidator<PermisoRequestDto>, BLL.Validators.PermisoValidator>();

        // ── 7b. Validadores Sprint 2 ─────────────────────────────────
        services.AddScoped<IValidator<PlanRequestDto>, BLL.Validators.PlanValidator>();
        services.AddScoped<IValidator<SuscripcionRequestDto>, BLL.Validators.SuscripcionValidator>();
        services.AddScoped<IValidator<PagoRequestDto>, BLL.Validators.PagoValidator>();
        services.AddScoped<IValidator<OnboardingPaso1RequestDto>, BLL.Validators.OnboardingPaso1Validator>();
        services.AddScoped<IValidator<OnboardingPaso3RequestDto>, BLL.Validators.OnboardingPaso3Validator>();
        services.AddScoped<IValidator<ConfiguracionRequestDto>, BLL.Validators.ConfiguracionValidator>();
        services.AddScoped<IValidator<NumeracionRequestDto>, BLL.Validators.NumeracionValidator>();

        // ── 7c. Validadores Sprint 3 (EP-03 Maestros y Catálogos) ────
        services.AddScoped<IValidator<UbicacionRequestDto>, BLL.Validators.UbicacionValidator>();
        services.AddScoped<IValidator<ZonaRequestDto>, BLL.Validators.ZonaEntregaValidator>();
        services.AddScoped<IValidator<TipoMercanciaRequestDto>, BLL.Validators.TipoMercanciaValidator>();
        services.AddScoped<IValidator<UnidadMedidaRequestDto>, BLL.Validators.UnidadMedidaValidator>();
        services.AddScoped<IValidator<TipoEmbalajeRequestDto>, BLL.Validators.TipoEmbalajeValidator>();
        services.AddScoped<IValidator<ClienteRequestDto>, BLL.Validators.ClienteValidator>();
        services.AddScoped<IValidator<TarifaBaseRequestDto>, BLL.Validators.TarifaBaseValidator>();

        return services;
    }
}