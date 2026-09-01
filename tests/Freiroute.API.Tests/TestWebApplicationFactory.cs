namespace Freiroute.API.Tests;

using Freiroute.BLL.Services;
using Freiroute.DTO.Empresa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

/// <summary>
/// Factory personalizada para pruebas de integración del API de Freiroute TMS.
/// Configura JWT con clave de testing, registra mock de servicios y provee
/// authorization policy provider para que todas las políticas existan en tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Lazy<Mock<IEmpresaService>> _mockService;

    public TestWebApplicationFactory()
    {
        _mockService = new Lazy<Mock<IEmpresaService>>(CreateDefaultMockService);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Inyectar configuraciones de JWT para testing (misma clave que JwtTestHelper)
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:Key"] = "EstaEsUnaClaveSecretaParaTesting2026SoloDevLocal",
                ["Jwt:Issuer"] = "freiroute-api",
                ["Jwt:Audience"] = "freiroute-client"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmpresaService));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddScoped(_ => _mockService.Value.Object);

            // Reemplazar policy provider — todas las políticas existen pero requieren user autenticado
            services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, TestPolicyProvider>());
        });

        return base.CreateHost(builder);
    }

    private static Mock<IEmpresaService> CreateDefaultMockService()
    {
        var mock = new Mock<IEmpresaService>();
        var responseDto = new EmpresaResponseDto
        {
            Id = Guid.NewGuid(),
            Nombre = "Transportes del Pacifico SA",
            Slug = "transportes-del-pacifico",
            Plan = "professional",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        mock.Setup(s => s.CrearAsync(It.IsAny<EmpresaRequestDto>())).ReturnsAsync(responseDto);
        return mock;
    }

    public Mock<IEmpresaService> GetMockService() => _mockService.Value;

    /// <summary>
    /// Crea un HttpClient autenticado con token Bearer de super admin.
    /// </summary>
    public HttpClient CreateClientWithToken(string? token = null)
    {
        var client = base.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var actualToken = token ?? JwtTestHelper.GenerateSuperAdminToken();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", actualToken);

        return client;
    }

    /// <summary>
    /// Crea un HttpClient sin autorización para probar endpoints que requieren auth.
    /// </summary>
    public HttpClient CreateUnauthenticatedClient() =>
        base.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    /// <summary>
    /// Policy provider que siempre retorna una política válida con RequireAuthenticatedUser.
    /// Sin handler propio: la validación real la hace el middleware JWT.
    /// Sin token -> 401 (por RequireAuthenticatedUser). Con token válido -> continúa al controller.
    /// </summary>
    private class TestPolicyProvider : IAuthorizationPolicyProvider
    {
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
            Task.FromResult<AuthorizationPolicy?>(null);

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            return Task.FromResult<AuthorizationPolicy?>(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
            Task.FromResult(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
    }
}
