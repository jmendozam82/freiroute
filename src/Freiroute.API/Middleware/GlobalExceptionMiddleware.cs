using System.Net;
using System.Text.Json;
using FluentValidation;
using Freiroute.Utility.ApiResponse;
using Freiroute.Utility.Exceptions;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Freiroute.API.Middleware;

/// <summary>
/// Middleware global de manejo de excepciones — PRIMERO en el pipeline.
/// Traduce las excepciones de negocio conocidas a respuestas HTTP con el
/// wrapper ApiResponse&lt;T&gt; (ADR-008) y devuelve 500 para errores inesperados
/// SIN filtrar el detalle interno hacia el cliente (solo se loguea, CA-04).
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            var errores = ex.Errors
                .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                .ToList();

            await EscribirErrorAsync(context, HttpStatusCode.BadRequest,
                "Error de validación", errores);
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Warning(ex, "Acceso no autenticado a {Ruta}", context.Request.Path);
            await EscribirErrorAsync(context, HttpStatusCode.Unauthorized,
                "No autenticado");
        }
        catch (NotFoundException ex)
        {
            await EscribirErrorAsync(context, HttpStatusCode.NotFound,
                ex.Message);
        }
        catch (ConflictException ex)
        {
            await EscribirErrorAsync(context, HttpStatusCode.Conflict,
                ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await EscribirErrorAsync(context, HttpStatusCode.Forbidden,
                ex.Message);
        }
        catch (BusinessException ex)
        {
            // 422 Unprocessable Entity: la petición es válida pero la regla
            // de negocio la rechaza (HU-001 a HU-008 / tabla del spec Sprint 1).
            await EscribirErrorAsync(context, HttpStatusCode.UnprocessableEntity,
                ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Excepción no controlada en {Ruta} — {Mensaje}",
                context.Request.Path, ex.Message);

            await EscribirErrorAsync(context, HttpStatusCode.InternalServerError,
                "Ocurrió un error interno en el servidor");
        }
    }

    private static async Task EscribirErrorAsync(
        HttpContext context, HttpStatusCode statusCode, string mensaje, List<string>? errores = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(
            ApiResponse<string>.Fail(mensaje, errores),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(body);
    }
}