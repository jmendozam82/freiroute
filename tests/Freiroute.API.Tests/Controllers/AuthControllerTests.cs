using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.Utility.Exceptions;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace Freiroute.API.Tests.Controllers;

/// <summary>
/// Tests de integración del AuthController (HU-003, HU-007).
/// NOTA de diseño: el AuthController es [AllowAnonymous] (toda la puerta de
/// entrada), por lo que logout/reset no requieren token. Las reglas de negocio
/// del login se verifican simulando las excepciones del IAuthService y la
/// respuesta del GlobalExceptionMiddleware (BusinessException → 422).
/// </summary>
public class AuthControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests()
    {
        _factory = new TestWebApplicationFactory();
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private static (bool Success, string Message) ParseRespuesta(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return (
            root.GetProperty("success").GetBoolean(),
            root.GetProperty("message").GetString() ?? string.Empty);
    }

    // ── POST /api/auth/login ──────────────────────────────────────

    [Fact]
    public async Task Login_SinBody_Retorna400()
    {
        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsync(
            "/api/auth/login", new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmailVacio_Retorna400ConErrores()
    {
        // El servicio devuelve ValidationException (LoginValidator) cuando el
        // email está vacío → el GlobalExceptionMiddleware responde 400 con errores.
        _factory.AuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ThrowsAsync(new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("Email", "El email es obligatorio")
            }));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "",
            Password = "MiPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("El email es obligatorio");
    }

    [Fact]
    public async Task Login_CredencialesValidas_Retorna200ConToken()
    {
        var usuario = new LoginResponseDto
        {
            AccessToken = "eyJ-test-access",
            RefreshToken = "refresh-test",
            ExpiresIn = 28800,
            Usuario = new UsuarioTokenDto
            {
                Id = Guid.NewGuid(),
                Nombre = "Juan Pérez",
                Email = "juan@transnic.com",
                TipoUsuario = "DISPATCHER",
                EmpresaNombre = "Trans Nicaragua S.A."
            }
        };

        _factory.AuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(usuario);

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "MiPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("eyJ-test-access");
    }

    [Fact]
    public async Task Login_CredencialesInvalidas_Retorna422()
    {
        // Credenciales inválidas → BusinessException → GlobalExceptionMiddleware → 422
        // (la convención del sistema para reglas de negocio; NO 400 como el label informal).
        _factory.AuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ThrowsAsync(new BusinessException("Credenciales inválidas"));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "juan@transnic.com",
            Password = "incorrecta"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Credenciales");
    }

    [Fact]
    public async Task Login_CuentaBloqueada_Retorna422ConMensaje()
    {
        // Cuenta bloqueada → BusinessException → 422 con mensaje de bloqueo.
        _factory.AuthService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequestDto>()))
            .ThrowsAsync(new BusinessException("Cuenta bloqueada hasta 02/09/2026 10:00"));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "bloqueado@transnic.com",
            Password = "MiPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("bloqueada");
    }

    // ── POST /api/auth/refresh ────────────────────────────────────

    [Fact]
    public async Task Refresh_TokenValido_Retorna200ConNuevoToken()
    {
        _factory.AuthService
            .Setup(s => s.RefreshAsync(It.IsAny<RefreshTokenRequestDto>()))
            .ReturnsAsync(new LoginResponseDto
            {
                AccessToken = "eyJ-nuevo-access",
                RefreshToken = "nuevo-refresh",
                ExpiresIn = 28800
            });

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
        {
            RefreshToken = "refresh-valido"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("eyJ-nuevo-access");
    }

    [Fact]
    public async Task Refresh_TokenInvalido_Retorna422()
    {
        _factory.AuthService
            .Setup(s => s.RefreshAsync(It.IsAny<RefreshTokenRequestDto>()))
            .ThrowsAsync(new BusinessException("Token inválido"));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto
        {
            RefreshToken = "token-malo"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── POST /api/auth/logout ─────────────────────────────────────

    [Fact]
    public async Task Logout_SinToken_Retorna200()
    {
        // El AuthController es [AllowAnonymous] → logout sin token no es 401;
        // es idempotente y devuelve 200 (documentado como desviación del label informal).
        _factory.AuthService
            .Setup(s => s.LogoutAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequestDto
        {
            RefreshToken = string.Empty
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ConToken_Retorna200()
    {
        _factory.AuthService
            .Setup(s => s.LogoutAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequestDto
        {
            RefreshToken = "refresh-a-revocar"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/auth/forgot-password ────────────────────────────

    [Fact]
    public async Task ForgotPassword_Retorna200SiempreAunqueEmailNoExiste()
    {
        // HU-007 CA-03: respuesta genérica idéntica — no revela si el email existe.
        _factory.AuthService.Setup(s => s.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequestDto
        {
            Email = "dondeNoExiste@empresa.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("recibirás un enlace");
    }

    // ── POST /api/auth/reset-password ─────────────────────────────

    [Fact]
    public async Task ResetPassword_TokenInvalido_Retorna422()
    {
        _factory.AuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .ThrowsAsync(new BusinessException("Token inválido o expirado"));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequestDto
        {
            Token = "token-invalido",
            NewPassword = "NuevaPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ResetPassword_PasswordDebil_Retorna400()
    {
        // Contraseña que no cumple la política → ValidationException → 400 con errores.
        _factory.AuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .ThrowsAsync(new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("NewPassword", "La contraseña debe contener al menos una mayúscula")
            }));

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequestDto
        {
            Token = "token",
            NewPassword = "password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("una may");
    }

    [Fact]
    public async Task ResetPassword_TokenValido_Retorna200()
    {
        _factory.AuthService
            .Setup(s => s.ResetPasswordAsync(It.IsAny<ResetPasswordRequestDto>()))
            .Returns(Task.CompletedTask);

        var client = _factory.CrearClientSinToken();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequestDto
        {
            Token = "token-valido",
            NewPassword = "NuevaPassword123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
