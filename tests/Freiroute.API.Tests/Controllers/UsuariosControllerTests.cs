using System.Net;
using System.Net.Http.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.DTO.Usuario;
using Freiroute.Utility.Exceptions;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del UsuariosController (HU-003).
/// RequirePermission sobre el módulo 'usuarios'. AceptarInvitacion es público
/// (AllowAnonymous) y valida el body (token).
/// </summary>
public class UsuariosControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public UsuariosControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private UsuarioResponseDto UsuarioDto() => new()
    {
        Id = Guid.NewGuid(),
        PerfilId = Guid.NewGuid(),
        PerfilNombre = "Operador",
        NombreCompleto = "Juan Pérez",
        Email = "juan@transnic.com",
        TipoUsuario = "OPERADOR",
        Estado = "ACTIVE",
        Activo = true,
        FechaCreacion = DateTime.UtcNow
    };

    [Fact]
    public async Task GetAll_SinToken_Retorna401()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.GetAsync("/api/usuarios");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ConPermiso_Retorna200()
    {
        _factory.UsuarioService
            .Setup(s => s.GetAllAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<UsuarioResponseDto> { UsuarioDto() });

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // usuarios:read

        var response = await client.GetAsync("/api/usuarios");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Invitar_EmailNuevo_Retorna201()
    {
        // TokenAdmin tiene 'usuarios:create'. El controller responde 201 Created.
        _factory.UsuarioService
            .Setup(s => s.InvitarAsync(
                It.IsAny<InvitacionRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/usuarios/invitar", new InvitacionRequestDto
        {
            Email = "nuevo@transnic.com",
            PerfilId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Invitar_EmailExistente_Retorna409()
    {
        _factory.UsuarioService
            .Setup(s => s.InvitarAsync(
                It.IsAny<InvitacionRequestDto>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new ConflictException("Ya existe un usuario con ese email en la empresa."));

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/usuarios/invitar", new InvitacionRequestDto
        {
            Email = "juan@transnic.com",
            PerfilId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AceptarInvitacion_TokenValido_Retorna201()
    {
        // Endpoint pública (AllowAnonymous) — no requiere token JWT. Responde 201 Created.
        _factory.UsuarioService
            .Setup(s => s.AceptarInvitacionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UsuarioDto());

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/usuarios/aceptar-invitacion", new ResetPasswordRequestDto
        {
            Token = "token-valido",
            NewPassword = "NuevaPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AceptarInvitacion_TokenExpirado_Retorna422()
    {
        // Token expirado → BusinessException → 422.
        _factory.UsuarioService
            .Setup(s => s.AceptarInvitacionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new BusinessException("Token inválido o expirado"));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/usuarios/aceptar-invitacion", new ResetPasswordRequestDto
        {
            Token = "expirado",
            NewPassword = "NuevaPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AceptarInvitacion_SinToken_Retorna400()
    {
        // Endpoint público que valida el body: sin token → error de validación → 400.
        _factory.UsuarioService
            .Setup(s => s.AceptarInvitacionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ValidationException(new[] {                 new FluentValidation.Results.ValidationFailure("Token", "El token es obligatorio") }));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/usuarios/aceptar-invitacion", new ResetPasswordRequestDto
        {
            Token = string.Empty,
            NewPassword = "NuevaPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/usuarios (Create) ──────────────────────────────────

    [Fact]
    public async Task Create_ConPermisoCreate_Retorna201()
    {
        var dto = UsuarioDto();
        _factory.UsuarioService
            .Setup(s => s.CreateAsync(It.IsAny<UsuarioRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(dto);

        // TokenAdmin tiene 'usuarios:create'
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenAdmin);

        var response = await client.PostAsJsonAsync("/api/usuarios", new UsuarioRequestDto
        {
            PerfilId = Guid.NewGuid(),
            NombreCompleto = "Juan Pérez",
            Email = "juan@transnic.com",
            TipoUsuario = "OPERADOR"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Juan Pérez");
    }

    [Fact]
    public async Task Create_SinPermiso_Retorna403()
    {
        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // solo read

        var response = await client.PostAsJsonAsync("/api/usuarios", new UsuarioRequestDto
        {
            PerfilId = Guid.NewGuid(),
            NombreCompleto = "Sin Permiso",
            Email = "sin@permiso.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── PUT /api/usuarios/{id} (Update) ───────────────────────────────

    [Fact]
    public async Task Update_ConPermisoUpdate_Retorna200()
    {
        var dto = UsuarioDto();
        _factory.UsuarioService
            .Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UsuarioRequestDto>(), It.IsAny<Guid>()))
            .ReturnsAsync(dto);

        // TokenAdmin no tiene 'usuarios:update', generamos un token con todos los permisos.
        var clientUpdate = _factory.CrearClientConToken(
            JwtTestHelper.GenerateTestToken(
                Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
                ["usuarios:read", "usuarios:create", "usuarios:update"], "ADMIN"));

        var response = await clientUpdate.PutAsJsonAsync($"/api/usuarios/{Guid.NewGuid()}", new UsuarioRequestDto
        {
            PerfilId = Guid.NewGuid(),
            NombreCompleto = "Juan Actualizado",
            Email = "juan@transnic.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("actualizado");
    }

    // ── GET /api/usuarios/{id} (GetById) ──────────────────────────────

    [Fact]
    public async Task GetById_ConPermisoRead_Retorna200()
    {
        var dto = UsuarioDto();
        _factory.UsuarioService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(dto);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // usuarios:read

        var response = await client.GetAsync($"/api/usuarios/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Juan Pérez");
    }

    [Fact]
    public async Task GetById_UsuarioNoExiste_Retorna404()
    {
        _factory.UsuarioService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((UsuarioResponseDto?)null);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura);

        var response = await client.GetAsync($"/api/usuarios/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/usuarios/by-email/{email} ────────────────────────────

    [Fact]
    public async Task GetByEmail_ConPermisoRead_Retorna200()
    {
        var dto = UsuarioDto();
        _factory.UsuarioService
            .Setup(s => s.GetByEmailAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(dto);

        var client = _factory.CrearClientConToken(JwtTestHelper.TokenSoloLectura); // usuarios:read

        var response = await client.GetAsync("/api/usuarios/by-email/juan@transnic.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("juan@transnic.com");
    }

    // ── PATCH /api/usuarios/{id}/deactivate ───────────────────────────

    [Fact]
    public async Task Deactivate_ConPermisoUpdate_Retorna200()
    {
        _factory.UsuarioService
            .Setup(s => s.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var clientUpdate = _factory.CrearClientConToken(
            JwtTestHelper.GenerateTestToken(
                Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
                ["usuarios:read", "usuarios:create", "usuarios:update"], "ADMIN"));

        var response = await clientUpdate.PatchAsync($"/api/usuarios/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("desactivado");
    }

    [Fact]
    public async Task Deactivate_UsuarioInexistente_Retorna422()
    {
        _factory.UsuarioService
            .Setup(s => s.DeactivateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ThrowsAsync(new BusinessException("Usuario no encontrado en la empresa"));

        var clientUpdate = _factory.CrearClientConToken(
            JwtTestHelper.GenerateTestToken(
                Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
                ["usuarios:read", "usuarios:create", "usuarios:update"], "ADMIN"));

        var response = await clientUpdate.PatchAsync($"/api/usuarios/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("no encontrado");
    }

    // ── PATCH /api/usuarios/{id}/reactivate ───────────────────────────

    [Fact]
    public async Task Reactivate_ConPermisoUpdate_Retorna200()
    {
        var dto = UsuarioDto();
        _factory.UsuarioService
            .Setup(s => s.ReactivarAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(dto);

        var clientUpdate = _factory.CrearClientConToken(
            JwtTestHelper.GenerateTestToken(
                Guid.NewGuid(), JwtTestHelper.EmpresaTenant,
                ["usuarios:read", "usuarios:create", "usuarios:update"], "ADMIN"));

        var response = await clientUpdate.PatchAsync($"/api/usuarios/{Guid.NewGuid()}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("reactivado");
    }
}
