using Freiroute.BLL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

    public TestWebApplicationFactory()
    {
        AuthService = new Mock<IAuthService>();
        EmpresaService = new Mock<IEmpresaService>();
        PerfilService = new Mock<IPerfilService>();
        PermisoService = new Mock<IPermisoService>();
        UsuarioService = new Mock<IUsuarioService>();
        AuditoriaService = new Mock<IAuditoriaService>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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

            services.AddSingleton(AuthService.Object);
            services.AddSingleton(EmpresaService.Object);
            services.AddSingleton(PerfilService.Object);
            services.AddSingleton(PermisoService.Object);
            services.AddSingleton(UsuarioService.Object);
            services.AddSingleton(AuditoriaService.Object);
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
