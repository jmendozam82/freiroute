using System.Data;
using Freiroute.BLL.Services;
using Freiroute.DAL.Repositories;
using Freiroute.DTO.Empresa;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Freiroute.IOC;

/// <summary>
 /// Centraliza el registro de dependencias DI para todas las capas del sistema Freiroute TMS.
 /// Este extensor inyecta conexiones BD, repositorios DAL, servicios BLL y validadores FluentValidation.
 /// Cada módulo nuevo se registra siguiendo esta estructura:
 ///  DB Connection → DAL Repositories → BLL Services → FluentValidation Validators
 /// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddFreirouteServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Conexión DB — IDbConnection instanciada con Npgsql (singleton compartido con Program.cs)
        services.AddSingleton<IDbConnection>(sp =>
            new NpgsqlConnection(configuration.GetConnectionString("SupabaseConnection")));

        // 2. Repositorios DAL
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();

        // 3. Servicios BLL
        services.AddScoped<IEmpresaService, EmpresaService>();

        // 4. Validadores FluentValidation
        services.AddScoped<IValidator<EmpresaRequestDto>, EmpresaValidator>();

        return services;
    }
}
