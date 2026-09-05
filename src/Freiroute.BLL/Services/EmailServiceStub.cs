using System.Text.RegularExpressions;
using Freiroute.BLL.Interfaces;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Stub de email para Sprint 1 (HU-001 CA-03, HU-003, HU-007 CA-02).
/// NO envía correos reales: solo loguea el mensaje con Serilog para que el
/// flujo end-to-end funcione sin infraestructura SMTP.
/// TODO (Sprint 2): reemplazar por cliente real (Supabase Edge Function o SMTP).
/// </summary>
public class EmailServiceStub : IEmailService
{
    private readonly ILogger<EmailServiceStub> _logger;

    public EmailServiceStub(ILogger<EmailServiceStub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loguea el email en lugar de enviarlo (stub Sprint 1).
    /// Fix re-smoke test: también loguea el CUERPO en texto plano (sin tags HTML)
    /// para que la contraseña temporal ("Contraseña temporal: Fr{XXXX}!") y los
    /// links de invitación sean observables en los logs durante las demos.
    /// </summary>
    public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        // Strip de tags HTML → texto plano legible en logs (Serilog JSON).
        var cuerpoTexto = Regex.Replace(cuerpoHtml, "<.*?>", " ");

        _logger.LogInformation(
            "╔══════════════════════════════════════════════════════════╗\n" +
            "║  EMAIL STUB (Sprint 1 — sin SMTP real)                   ║\n" +
            "╠══════════════════════════════════════════════════════════╣\n" +
            "║  Para:   {Destinatario}                                   ║\n" +
            "║  Asunto: {Asunto}                                         ║\n" +
            "╚══════════════════════════════════════════════════════════╝\n" +
            "Cuerpo: {CuerpoTexto}",
            destinatario, asunto, cuerpoTexto);

        return Task.CompletedTask;
    }
}