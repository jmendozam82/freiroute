using Freiroute.API.Middleware;
using Freiroute.IOC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

namespace Freiroute.API;

/// <summary>
/// Entry point principal del servidor API de Freiroute TMS.
/// Configura logging (Serilog), DI centralizada (IOC), Auth JWT, Swagger con
/// Bearer y el pipeline de middleware (orden crítico — Fase 3 Sprint 1).
/// Patrón explícito para compatibilidad con WebApplicationFactory en tests de integración.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        var builder = CreateBuilder(args);
        var app = CreateApp(builder);
        app.Run();
    }

    /// <summary>
    /// Crea y configura el WebApplicationBuilder con todas las dependencias.
    /// </summary>
    public static WebApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuración Logging (Serilog)
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        // --- DI Centralizada (IOC Layer) ---
        builder.Services.AddFreirouteServices(builder.Configuration);

        // --- Auth JWT Base ---
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            var xmlFile = $"{typeof(Program).Namespace}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

            // Esquema de seguridad Bearer para probar los endpoints desde Swagger
            // (documenta el header Authorization: Bearer {token}).
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese el JWT: Bearer {token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return builder;
    }

    /// <summary>
    /// Construye y configura el pipeline de la aplicación web.
    /// Orden crítico (Fase 3 Sprint 1):
    ///   GlobalExceptionMiddleware (1º) → HTTPS → Auth → Authorization →
    ///   TenantMiddleware (último antes de los controllers) → MapControllers
    /// </summary>
    public static WebApplication CreateApp(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        // 1. Manejo global de excepciones — SIEMPRE el primero del pipeline,
        //    para que ningún error de etapas posteriores escape del wrapper ApiResponse<T>.
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();           // 1. Valida token JWT
        app.UseAuthorization();            // 2. Verifica [Authorize] / [RequirePermission]
        app.UseMiddleware<TenantMiddleware>(); // 3. Resuelve empresa_id → RLS session

        app.MapControllers();

        // Health check público (ruta excluida de TenantMiddleware).
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow
        }));

        try
        {
            Log.Information("Iniciando servidor API de Freiroute TMS...");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "El servidor falló al iniciarse correctamente");
        }

        return app;
    }
}