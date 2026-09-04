using Freiroute.API.Attributes;
using Freiroute.API.Extensions;
using Freiroute.BLL.Interfaces;
using Freiroute.DTO.Auth;
using Freiroute.DTO.Usuario;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Freiroute.API.Controllers;

/// <summary>
/// Endpoints de usuarios del tenant (HU-003, HU-004).
/// El empresa_id SIEMPRE viene del JWT (o header X-Empresa-Id para SUPER_ADMIN),
/// nunca del body. El alta por invitación (POST invitar) auditada con creadoPorId.
/// </summary>
[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    /// <summary>Obtiene todos los usuarios activos de la empresa con su perfil.</summary>
    [HttpGet]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Read)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UsuarioResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UsuarioResponseDto>>>> GetAll()
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuarios = await _usuarioService.GetAllAsync(empresaId);
        return Ok(ApiResponse<IEnumerable<UsuarioResponseDto>>.Ok(usuarios));
    }

    /// <summary>Obtiene un usuario activo por Id dentro de la empresa.</summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<UsuarioResponseDto>>> GetById(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuario = await _usuarioService.GetByIdAsync(id, empresaId);
        return usuario is null
            ? NotFound(ApiResponse<string>.Fail($"Usuario con id '{id}' no encontrado."))
            : Ok(ApiResponse<UsuarioResponseDto>.Ok(usuario));
    }

    /// <summary>Obtiene un usuario activo por email dentro de la empresa.</summary>
    [HttpGet("by-email/{email}")]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Read)]
    public async Task<ActionResult<ApiResponse<UsuarioResponseDto>>> GetByEmail(string email)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuario = await _usuarioService.GetByEmailAsync(email, empresaId);
        return usuario is null
            ? NotFound(ApiResponse<string>.Fail($"Usuario '{email}' no encontrado."))
            : Ok(ApiResponse<UsuarioResponseDto>.Ok(usuario));
    }

    /// <summary>Crea un usuario en estado PENDING (debe activar/aceptar invitación).</summary>
    [HttpPost]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UsuarioResponseDto>>> Create(UsuarioRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuario = await _usuarioService.CreateAsync(request, empresaId);
        return CreatedAtAction(
            nameof(GetById),
            new { id = usuario.Id },
            ApiResponse<UsuarioResponseDto>.Ok(usuario, "Usuario creado exitosamente"));
    }

    /// <summary>Actualiza un usuario activo de la empresa (el email debe seguir siendo único).</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<UsuarioResponseDto>>> Update(Guid id, UsuarioRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var usuario = await _usuarioService.UpdateAsync(id, request, empresaId);
        return Ok(ApiResponse<UsuarioResponseDto>.Ok(usuario, "Usuario actualizado"));
    }

    /// <summary>Soft delete de un usuario. Nunca se elimina físicamente.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Update)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        await _usuarioService.DeactivateAsync(id, empresaId);
        return Ok(ApiResponse<string>.Ok(string.Empty, "Usuario desactivado"));
    }

    /// <summary>Reactiva un usuario previamente desactivado (HU-013 CA-07). Verifica el límite del plan.</summary>
    [HttpPatch("{id:guid}/reactivate")]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Update)]
    public async Task<ActionResult<ApiResponse<UsuarioResponseDto>>> Reactivate(Guid id)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var reactivadoPorId = User.GetUsuarioId();
        var usuario = await _usuarioService.ReactivarAsync(id, empresaId, reactivadoPorId);
        return Ok(ApiResponse<UsuarioResponseDto>.Ok(usuario, "Usuario reactivado"));
    }

    /// <summary>Invita a un usuario por email (crea la cuenta PENDING + token 48 h + email).</summary>
    [HttpPost("invitar")]
    [RequirePermission(ModuloPermiso.Usuarios, PermissionType.Create)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Invitar(InvitacionRequestDto request)
    {
        var empresaId = User.GetTenantEfectivo(HttpContext);
        var creadoPorId = User.GetUsuarioId();
        await _usuarioService.InvitarAsync(request, empresaId, creadoPorId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<string>.Ok(string.Empty, "Invitación enviada exitosamente"));
    }

    /// <summary>Endpoint público: acepta la invitación con el token de 48 horas (HU-003 CA-03).</summary>
    [AllowAnonymous]
    [HttpPost("aceptar-invitacion")]
    [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UsuarioResponseDto>>> AceptarInvitacion(ResetPasswordRequestDto request)
    {
        var usuario = await _usuarioService.AceptarInvitacionAsync(request.Token, request.NewPassword);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UsuarioResponseDto>.Ok(usuario, "Cuenta activada exitosamente"));
    }
}