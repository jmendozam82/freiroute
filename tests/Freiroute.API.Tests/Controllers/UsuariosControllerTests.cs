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
    public async Task Invitar_EmailNuevo_Retorna200()
    {
        // TokenAdmin tiene 'usuarios:create'. El controller responde Ok() = 200 (no 201).
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

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
    public async Task AceptarInvitacion_TokenValido_Retorna200()
    {
        // Endpoint pública (AllowAnonymous) — no requiere token JWT.
        _factory.UsuarioService
            .Setup(s => s.AceptarInvitacionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(UsuarioDto());

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/usuarios/aceptar-invitacion", new ResetPasswordRequestDto
        {
            Token = "token-valido",
            NewPassword = "NuevaPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
}
