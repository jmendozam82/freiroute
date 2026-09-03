using Freiroute.Entity;

namespace Freiroute.DAL.Interfaces;

/// <summary>
/// Contrato de acceso a datos de la tabla 'invitaciones' (HU-003 invitaciones,
/// HU-007 recuperación de contraseña).
/// NOTA: esta interfaz NO existía en la entrega de Fase 2 de @IngenieroDatos;
/// fue agregada por @BackendDev (Fase 3) porque los servicios InvitarAsync,
/// AceptarInvitacionAsync, ForgotPasswordAsync y ResetPasswordAsync la requieren.
/// La tabla 'invitaciones' SÍ existe (migración 20260101000006).
/// </summary>
public interface IInvitacionRepository
{
    /// <summary>Inserta una invitación. El UUID lo genera la BD. Token UNIQUE (un solo uso).</summary>
    Task<Guid> CreateAsync(Invitacion invitacion);

    /// <summary>Obtiene una invitación por su token (lookup global — el token es UNIQUE en la BD).</summary>
    Task<Invitacion?> GetByTokenAsync(string token);

    /// <summary>
    /// Marca una invitación como aceptada: estado = 'ACCEPTED' y
    /// fecha_aceptacion = @FechaAceptacion. El token es de un solo uso (CA-04).
    /// </summary>
    Task<bool> MarcarAceptadaAsync(Guid id, DateTime fechaAceptacion);

    /// <summary>Marca una invitación como expirada: estado = 'EXPIRED'.</summary>
    Task<bool> MarcarExpiradaAsync(Guid id);
}