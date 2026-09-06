using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Freiroute.BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Freiroute.BLL.Services;

/// <summary>
/// Implementación de envío de correos utilizando la API de Resend (Sprint 2/Fase 2).
/// </summary>
public class ResendEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _fromEmail;

    public ResendEmailService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Resend:ApiKey"] ?? string.Empty;
        _fromEmail = configuration["Resend:FromEmail"] ?? "onboarding@resend.dev";

        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Resend API key no está configurada. El correo a {Destinatario} no se envió.", destinatario);
            return;
        }

        var requestBody = new
        {
            from = _fromEmail,
            to = new[] { destinatario },
            subject = asunto,
            html = cuerpoHtml
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        try
        {
            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Correo enviado exitosamente a {Destinatario} usando Resend.", destinatario);
            }
            else
            {
                _logger.LogError("Fallo al enviar correo a {Destinatario}. Status: {StatusCode}. Error: {Error}", 
                    destinatario, response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al intentar enviar correo a {Destinatario} usando Resend.", destinatario);
        }
    }
}
