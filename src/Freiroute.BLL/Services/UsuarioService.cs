using FluentValidation;
using Freiroute.BLL.Interfaces;
using Freiroute.BLL.Settings;
using Freiroute.DAL.Interfaces;
using Freiroute.DTO.Usuario;
using Freiroute.Entity;
using Freiroute.Utility.Constants;
using Freiroute.Utility.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Freiroute.BLL.Services;

/// <summary>
/// Lógica de negocio de usuarios por tenant (HU-003, HU-004).
/// - CreateAsync: crea el usuario en PENDING (sin email — flujo interno).
/// - InvitarAsync: crea el usuario PENDING + invita por email con token de 48 h.
/// - AceptarInvitacionAsync: valida token de un solo uso, crea la identidad en
///   Supabase Auth (si no existe), activa el usuario y registra la auditoría.
/// Todo método recibe empresaId del JWT — nunca del body (regla de Fase 2).
/// </summary>
public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPerfilRepository _perfilRepository;
    private readonly IInvitacionRepository _invitacionRepository;
    private readonly IValidator<UsuarioRequestDto> _validator;
    private readonly ISupabaseAuthService _supabaseAuth;
    private readonly IAuditoriaService _auditoria;
    private readonly IEmailService _emailService;
    private readonly IPlanLimiteService _planLimiteService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<UsuarioService> _logger;

    // Vida útil del token de invitación (HU-003 / spec invitaciones).
    private static readonly TimeSpan InvitacionExpiracion = TimeSpan.FromHours(48);

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IPerfilRepository perfilRepository,
        IInvitacionRepository invitacionRepository,
        IValidator<UsuarioRequestDto> validator,
        ISupabaseAuthService supabaseAuth,
        IAuditoriaService auditoria,
        IEmailService emailService,
        IPlanLimiteService planLimiteService,
        IOptions<AppSettings> appSettings,
        ILogger<UsuarioService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _perfilRepository = perfilRepository;
        _invitacionRepository = invitacionRepository;
        _validator = validator;
        _supabaseAuth = supabaseAuth;
        _auditoria = auditoria;
        _emailService = emailService;
        _planLimiteService = planLimiteService;
        _appSettings = appSettings.Value;
        _logger = logger;
    }

    // ── Consultas (HU-003) ─────────────────────────────────────────

    /// <summary>Obtiene los usuarios activos de la empresa con el nombre de su perfil.</summary>
    public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(Guid empresaId)
    {
        var usuarios = await _usuarioRepository.GetAllAsync(empresaId);
        return await MapUsuariosAsync(empresaId, usuarios);
    }

    /// <summary>Obtiene un usuario activo por Id dentro de la empresa.</summary>
    public async Task<UsuarioResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id, empresaId);
        return usuario is null ? null : await MapUsuarioAsync(empresaId, usuario);
    }

    /// <summary>Obtiene un usuario activo por email dentro de la empresa.</summary>
    public async Task<UsuarioResponseDto?> GetByEmailAsync(string email, Guid empresaId)
    {
        var usuario = await _usuarioRepository.GetByEmailAsync(email, empresaId);
        return usuario is null ? null : await MapUsuarioAsync(empresaId, usuario);
    }

    // ── Crear / Actualizar / Desactivar (HU-003) ───────────────────

    /// <summary>Crea un usuario en estado PENDING (activación pendiente de aceptar invitación).</summary>
    public async Task<UsuarioResponseDto> CreateAsync(UsuarioRequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        // HU-013 CA-08: verificar el límite de usuarios del plan ANTES de crear
        // (Fix re-smoke test — antes solo se verificaba en ReactivarAsync).
        await _planLimiteService.VerificarLimiteUsuariosAsync(empresaId);

        await ValidarPerfilAsync(empresaId, dto.PerfilId);
        await ValidarEmailUnicoAsync(empresaId, dto.Email, null);

        // El SUPER_ADMIN no se crea por tenant (exclusivo de la empresa raíz).
        ValidarTipoUsuarioPermitido(dto.TipoUsuario);

        var usuario = MapToEntity(dto, empresaId);
        var usuarioId = await _usuarioRepository.CreateAsync(usuario);

        await _auditoria.RegistrarAsync(
            "usuarios", AccionAuditoria.CREATE, empresaId, null,
            nameof(Usuario), usuarioId,
            new { dto.Email, perfilId = dto.PerfilId, estado = EstadoUsuario.PENDING });

        usuario.Id = usuarioId;
        return await MapUsuarioAsync(empresaId, usuario);
    }

    /// <summary>Actualiza un usuario activo de la empresa (sin email duplicado).</summary>
    public async Task<UsuarioResponseDto> UpdateAsync(Guid id, UsuarioRequestDto dto, Guid empresaId)
    {
        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var existente = await _usuarioRepository.GetByIdAsync(id, empresaId);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Usuario), id);
        }

        await ValidarPerfilAsync(empresaId, dto.PerfilId);
        await ValidarEmailUnicoAsync(empresaId, dto.Email, id);
        ValidarTipoUsuarioPermitido(dto.TipoUsuario);

        // Conservar los campos de seguridad que el cliente NO puede modificar.
        existente.PerfilId = dto.PerfilId;
        existente.NombreCompleto = dto.NombreCompleto;
        existente.Email = dto.Email;
        existente.Telefono = dto.Telefono;
        existente.FotoUrl = dto.FotoUrl;
        existente.TipoUsuario = dto.TipoUsuario;
        existente.TipoIdentidad = dto.TipoIdentidad ?? existente.TipoIdentidad;
        existente.NumeroIdentidad = dto.NumeroIdentidad;
        existente.FechaModificacion = DateTime.UtcNow;

        var ok = await _usuarioRepository.UpdateAsync(existente);
        if (!ok)
        {
            throw new NotFoundException(nameof(Usuario), id);
        }

        await _auditoria.RegistrarAsync(
            "usuarios", AccionAuditoria.UPDATE, empresaId, null,
            nameof(Usuario), id, new { dto.Email, perfilId = dto.PerfilId });

        return await MapUsuarioAsync(empresaId, existente);
    }

    /// <summary>Soft delete de un usuario. Nunca elimina físicamente (CA-09).</summary>
    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        var existente = await _usuarioRepository.GetByIdAsync(id, empresaId);
        if (existente is null)
        {
            throw new NotFoundException(nameof(Usuario), id);
        }

        var ok = await _usuarioRepository.DeactivateAsync(id, empresaId);
        if (!ok)
        {
            throw new NotFoundException(nameof(Usuario), id);
        }

        await _auditoria.RegistrarAsync(
            "usuarios", AccionAuditoria.DEACTIVATE, empresaId, null,
            nameof(Usuario), id, new { email = existente.Email });

        return true;
    }

    /// <summary>
    /// Reactiva un usuario previamente desactivado (HU-013 CA-07).
    /// Verifica el límite de usuarios del plan (CA-08) antes de reactivar
    /// y retorna el UsuarioResponseDto actualizado.
    /// </summary>
    public async Task<UsuarioResponseDto> ReactivarAsync(
        Guid id, Guid empresaId, Guid reactivadoPorId)
    {
        // 1. Verificar que existe (activo o inactivo) — GetByIdIncluyendoInactivosAsync.
        var usuario = await _usuarioRepository
            .GetByIdIncluyendoInactivosAsync(id, empresaId);

        if (usuario is null)
        {
            throw new NotFoundException(nameof(Usuario), id);
        }

        if (usuario.Activo && usuario.Estado == EstadoUsuario.ACTIVE)
        {
            throw new BusinessException("El usuario ya está activo.");
        }

        // 2. Verificar límite del plan ANTES de reactivar (CA-08).
        await _planLimiteService.VerificarLimiteUsuariosAsync(empresaId);

        // 3. Reactivar.
        usuario.Activo = true;
        usuario.Estado = EstadoUsuario.ACTIVE;
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.FechaModificacion = DateTime.UtcNow;
        var ok = await _usuarioRepository.UpdateAsync(usuario);
        if (!ok)
        {
            // UpdateAsync filtra activo=true; para un usuario inactivo se usa
            // ReactivarAsync del repositorio si el UPDATE no afectó filas.
            await _usuarioRepository.ReactivarAsync(id, empresaId);
        }

        // 4. Auditoría.
        await _auditoria.RegistrarAsync(
            "usuarios", AccionAuditoria.REACTIVAR, empresaId, reactivadoPorId,
            nameof(Usuario), id, new { reactivado = true });

        // 5. Retornar UsuarioResponseDto actualizado.
        var actualizado = await _usuarioRepository.GetByIdAsync(id, empresaId)
            ?? await _usuarioRepository.GetByIdIncluyendoInactivosAsync(id, empresaId);
        return await MapUsuarioAsync(empresaId, actualizado!);
    }

    // ── Invitación por email (HU-003 CA-03) ────────────────────────

    /// <summary>
    /// Invita a un usuario: crea la cuenta en PENDING y envía un email con un
    /// token de activación de 48 horas. El creador (creadoPorId) queda auditado.
    /// </summary>
    public async Task InvitarAsync(InvitacionRequestDto dto, Guid empresaId, Guid creadoPorId)
    {
        // HU-013 CA-08: verificar el límite de usuarios del plan al inicio
        // (Fix re-smoke test — antes solo se verificaba en ReactivarAsync).
        await _planLimiteService.VerificarLimiteUsuariosAsync(empresaId);

        if (string.IsNullOrWhiteSpace(dto.Email) || !IsValidEmail(dto.Email))
        {
            throw new BusinessException("El email no tiene un formato válido");
        }

        var perfil = await ValidarPerfilAsync(empresaId, dto.PerfilId);
        await ValidarEmailUnicoAsync(empresaId, dto.Email, null);

        // Crear el usuario en PENDING (no puede iniciar sesión hasta aceptar).
        var usuario = new Usuario
        {
            EmpresaId = empresaId,
            PerfilId = dto.PerfilId,
            NombreCompleto = DerivarNombreDeEmail(dto.Email),
            Email = dto.Email,
            TipoUsuario = DerivarTipoUsuario(perfil.TipoPerfil),
            Estado = EstadoUsuario.PENDING,
            FechaCreacion = DateTime.UtcNow
        };

        var usuarioId = await _usuarioRepository.CreateAsync(usuario);

        // Token de activación de un solo uso, 48 horas.
        var token = Guid.NewGuid().ToString("N");
        await _invitacionRepository.CreateAsync(new Invitacion
        {
            EmpresaId = empresaId,
            Email = dto.Email,
            PerfilId = dto.PerfilId,
            Token = token,
            Estado = "PENDING",
            FechaExpiracion = DateTime.UtcNow.Add(InvitacionExpiracion),
            CreadoPorId = creadoPorId,
            FechaCreacion = DateTime.UtcNow
        });

        var link = $"{_appSettings.BaseUrl}/auth/aceptar-invitacion?token={token}";
        await _emailService.EnviarAsync(
            dto.Email,
            "Invitación a Freiroute TMS",
            "<p>Te han invitado a unirte a la plataforma de gestión de transporte.</p>" +
            $"<p><a href=\"{link}\">Aceptar invitación</a> (válido por 48 horas)</p>");

        // HU-003 CA-08: registro de la invitación.
        await _auditoria.RegistrarAsync(
            "usuarios", "INVITE", empresaId, creadoPorId,
            nameof(Usuario), usuarioId,
            new { dto.Email, perfilId = dto.PerfilId, expiraEnHoras = 48 });
    }

    /// <summary>
    /// Acepta la invitación (HU-003 CA-03): valida el token de un solo uso,
    /// crea la identidad en Supabase Auth si no existía, activa el usuario,
    /// invalida el token y registra la auditoría.
    /// </summary>
    public async Task<UsuarioResponseDto> AceptarInvitacionAsync(string token, string nuevaPassword)
    {
        var invitacion = await _invitacionRepository.GetByTokenAsync(token);

        if (invitacion is null ||
            invitacion.Estado != "PENDING" ||
            invitacion.FechaExpiracion < DateTime.UtcNow)
        {
            throw new BusinessException("Token inválido o expirado");
        }

        var usuario = await _usuarioRepository.GetByEmailAsync(
            invitacion.Email, invitacion.EmpresaId);

        if (usuario is null)
        {
            throw new BusinessException("Token inválido o expirado");
        }

        // Crear la identidad en Supabase Auth la primera vez (stub Sprint 1).
        if (!usuario.SupabaseUserId.HasValue)
        {
            var supabaseUserId = await _supabaseAuth.SignUpAsync(invitacion.Email, nuevaPassword);
            usuario.SupabaseUserId = supabaseUserId;
        }

        // Activar la cuenta y cerrar el ciclo de la invitación.
        usuario.Estado = EstadoUsuario.ACTIVE;
        usuario.FechaModificacion = DateTime.UtcNow;
        await _usuarioRepository.UpdateAsync(usuario);

        await _invitacionRepository.MarcarAceptadaAsync(invitacion.Id, DateTime.UtcNow);

        await _auditoria.RegistrarAsync(
            "usuarios", "INVITE_ACCEPTED", usuario.EmpresaId, usuario.Id,
            nameof(Usuario), usuario.Id,
            new { email = usuario.Email });

        return await MapUsuarioAsync(usuario.EmpresaId, usuario);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task<Perfil> ValidarPerfilAsync(Guid empresaId, Guid perfilId)
    {
        var perfil = await _perfilRepository.GetByIdAsync(perfilId, empresaId);
        if (perfil is null || !perfil.Activo)
        {
            throw new NotFoundException(nameof(Perfil), perfilId);
        }

        return perfil;
    }

    /// <summary>Valida que el email no esté duplicado dentro de la empresa (HU-003 CA-03 → 409).</summary>
    private async Task ValidarEmailUnicoAsync(Guid empresaId, string email, Guid? excluyendoId)
    {
        var existente = await _usuarioRepository.GetByEmailAsync(email, empresaId);
        if (existente is not null && existente.Id != excluyendoId)
        {
            throw new ConflictException("Ya existe un usuario con ese email en la empresa.");
        }
    }

    /// <summary>El SUPER_ADMIN solo se crea por migración/seeding en la empresa raíz — nunca por request.</summary>
    private static void ValidarTipoUsuarioPermitido(string tipoUsuario)
    {
        string[] tiposValidos =
        [
            TipoUsuario.ADMIN,
            TipoUsuario.DISPATCHER,
            TipoUsuario.OPERADOR,
            TipoUsuario.CONDUCTOR,
            TipoUsuario.CLIENTE
        ];

        if (tipoUsuario == TipoUsuario.SUPER_ADMIN)
        {
            throw new BusinessException("No se puede asignar el rol de Super Admin.");
        }

        if (!tiposValidos.Contains(tipoUsuario))
        {
            throw new BusinessException("El tipo de usuario no es válido.");
        }
    }

    private static Usuario MapToEntity(UsuarioRequestDto dto, Guid empresaId) => new()
    {
        EmpresaId = empresaId,
        PerfilId = dto.PerfilId,
        NombreCompleto = dto.NombreCompleto,
        Email = dto.Email,
        Telefono = dto.Telefono,
        FotoUrl = dto.FotoUrl,
        TipoIdentidad = string.IsNullOrWhiteSpace(dto.TipoIdentidad) ? "CEDULA" : dto.TipoIdentidad!,
        NumeroIdentidad = dto.NumeroIdentidad,
        TipoUsuario = dto.TipoUsuario,
        Estado = EstadoUsuario.PENDING, // Debe activar/aceptar invitación
        FechaCreacion = DateTime.UtcNow
    };

    private async Task<IEnumerable<UsuarioResponseDto>> MapUsuariosAsync(
        Guid empresaId, IEnumerable<Usuario> usuarios)
    {
        var result = new List<UsuarioResponseDto>();
        foreach (var usuario in usuarios)
        {
            result.Add(await MapUsuarioAsync(empresaId, usuario));
        }

        return result;
    }

    private async Task<UsuarioResponseDto> MapUsuarioAsync(Guid empresaId, Usuario usuario)
    {
        var perfil = await _perfilRepository.GetByIdAsync(usuario.PerfilId, empresaId);

        return new UsuarioResponseDto
        {
            Id = usuario.Id,
            PerfilId = usuario.PerfilId,
            PerfilNombre = perfil?.Nombre,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email,
            Telefono = usuario.Telefono,
            FotoUrl = usuario.FotoUrl,
            TipoUsuario = usuario.TipoUsuario,
            Estado = usuario.Estado,
            UltimoAcceso = usuario.UltimoAcceso,
            Activo = usuario.Activo,
            FechaCreacion = usuario.FechaCreacion
        };
    }

    /// <summary>
    /// Deriva el nombre inicial de la parte local del email (invitación):
    /// convierte puntos/guiones en espacios y capitaliza cada palabra.
    /// Ejemplo: juan.perez@x.com → "Juan Perez".
    /// </summary>
    private static string DerivarNombreDeEmail(string email)
    {
        var local = email.Split('@')[0];
        var palabras = local
            .Split(['.', '_', '-', '+'], StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Any(char.IsLetterOrDigit));

        return string.Join(" ", palabras.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    /// <summary>Deriva el TipoUsuario del perfil base asignado en la invitación.</summary>
    private static string DerivarTipoUsuario(string tipoPerfil) => tipoPerfil switch
    {
        TipoPerfil.ADMIN => TipoUsuario.ADMIN,
        TipoPerfil.DISPATCHER => TipoUsuario.DISPATCHER,
        TipoPerfil.OPERADOR => TipoUsuario.OPERADOR,
        TipoPerfil.CONDUCTOR => TipoUsuario.CONDUCTOR,
        TipoPerfil.CLIENTE => TipoUsuario.CLIENTE,
        _ => TipoUsuario.OPERADOR
    };

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}