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

    /// <summary>Loguea el email en lugar de enviarlo (stub Sprint 1).</summary>
    public Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        _logger.LogInformation(
            "EMAIL STUB → Para: {Destinatario} | Asunto: {Asunto}",
            destinatario, asunto);

        return Task.CompletedTask;
    }
}