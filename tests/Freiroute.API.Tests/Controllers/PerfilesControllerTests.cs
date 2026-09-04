using System.Net;
using System.Net.Http.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Permiso;
using Freiroute.DTO.Perfil;
using Freiroute.Utility.Exceptions;
using FluentAssertions;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del PerfilesController (HU-006).
/// RequirePermission sobre el módulo 'configuracion' (read/create/update).
/// Las reglas de negocio lanzan BusinessException → 422 (GlobalExceptionMiddleware).
/// </summary>
public class PerfilesControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public PerfilesControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private string TokenConConfiguracionCreate =>
        JwtTestHelper.GenerateTestToken(Guid.NewGuid(), Guid.NewGuid(), new[] { "configuracion:create" }, "ADMIN");

    private string TokenConConfiguracionUpdate =>
        JwtTestHelper.GenerateTestToken(Guid.NewGuid(), Guid.NewGuid(), new[] { "configuracion:update" }, "ADMIN");

    [Fact]
    public async Task GetAll_SinPermiso_Retorna403()
    {
        // OPERADOR sin permisos → RequirePermission no encuentra 'configuracion:read'.
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSinPermisos);

        var response = await client.GetAsync("/api/perfiles");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_ConPermiso_Retorna200()
    {
        _factory.PerfilService
            .Setup(s => s.GetAllAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<PerfilResponseDto>
            {
                new() { Id = Guid.NewGuid(), Nombre = "Admin", TipoPerfil = "ADMIN", Activo = true, FechaCreacion = DateTime.UtcNow }
            });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // configuracion:read

        var response = await client.GetAsync("/api/perfiles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_Valido_Retorna201()
    {
        // Se requiere 'configuracion:create'. El controller responde 201 Created.
        _factory.PerfilService
            .Setup(s => s.CreateAsync(It.IsAny<PerfilRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(new PerfilResponseDto
            {
                Id = Guid.NewGuid(),
                Nombre = "Operador Avanzado",
                TipoPerfil = "CUSTOM",
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            });

        var client = _factory.CrearClientConToken(TokenConConfiguracionCreate);

        var response = await client.PostAsJsonAsync("/api/perfiles", new PerfilRequestDto
        {
            Nombre = "Operador Avanzado",
            Descripcion = "Perfil personalizado"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Deactivate_PerfilSistema_Retorna422()
    {
        // Los perfiles de sistema no se desactivan → BusinessException → 422.
        _factory.PerfilService
            .Setup(s => s.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new BusinessException("No se puede desactivar un perfil del sistema."));

        var client = _factory.CrearClientConToken(TokenConConfiguracionUpdate);

        var response = await client.PatchAsync(
            $"/api/perfiles/{Guid.NewGuid()}/deactivate",
            new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PutPermisos_PerfilValido_Retorna200()
    {
        _factory.PermisoService
            .Setup(s => s.ReemplazarPermisosAsync(It.IsAny<Guid>(), It.IsAny<PermisoRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var client = _factory.CrearClientConToken(TokenConConfiguracionUpdate);

        var response = await client.PutAsJsonAsync(
            $"/api/perfiles/{Guid.NewGuid()}/permisos",
            new PermisoRequestDto
            {
                PerfilId = Guid.NewGuid(),
                Modulos = new List<ModuloPermisoRequestDto>
                {
                    new() { Modulo = "embarques", PuedeLeer = true, PuedeCrear = true, PuedeActualizar = true }
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutPermisos_SuperAdmin_Retorna422()
    {
        // Blindaje: no se modifican los permisos del Super Admin → BusinessException → 422.
        _factory.PermisoService
            .Setup(s => s.ReemplazarPermisosAsync(It.IsAny<Guid>(), It.IsAny<PermisoRequestDto>(), It.IsAny<Guid>()))
            .ThrowsAsync(new BusinessException("No se pueden modificar los permisos del Super Admin."));

        var client = _factory.CrearClientConToken(TokenConConfiguracionUpdate);

        var response = await client.PutAsJsonAsync(
            $"/api/perfiles/{Guid.NewGuid()}/permisos",
            new PermisoRequestDto
            {
                PerfilId = Guid.NewGuid(),
                Modulos = new List<ModuloPermisoRequestDto>
                {
                    new() { Modulo = "embarques", PuedeLeer = true }
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
