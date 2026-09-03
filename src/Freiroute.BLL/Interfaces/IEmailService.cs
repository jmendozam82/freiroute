namespace Freiroute.BLL.Interfaces;

/// <summary>
/// Contrato de envío de emails del sistema (HU-001 CA-03 bienvenida,
/// HU-003 invitación/activación, HU-007 recuperación de contraseña).
/// En Sprint 1 se usa EmailServiceStub que solo loguea — el envío real
/// (Supabase Edge Function / SMTP) va en Sprint 2.
/// </summary>
public interface IEmailService
{
    /// <summary>Envía un email en HTML.</summary>
    /// <param name="destinatario">Email del destinatario.</param>
    /// <param name="asunto">Asunto del correo.</param>
    /// <param name="cuerpoHtml">Cuerpo del correo en HTML.</param>
    Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml);
}