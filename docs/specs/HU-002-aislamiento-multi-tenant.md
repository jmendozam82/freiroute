# Spec: HU-002 — Aislamiento de datos por tenant (Row Level Security + Middleware)

## Historia de Usuario
**Como** arquitecto del sistema,  
**quiero** que cada tenant acceda únicamente a sus propios datos,  
**para** garantizar aislamiento total entre empresas sin depender exclusivamente de filtros manuales.

> ⚠️ **Regla Crítica:** 
> - `SUPER_ADMIN` ignora el filtro `empresa_id` y tiene visibilidad global.
> - `ADMIN` y roles operativos son estrictamente filtrados por `empresa_id`.
> - Nunca exponer IDs crudos de otros tenants en respuestas.

## Arquitectura de Aislamiento (Defense in Depth)
El sistema implementa dos capas obligatorias de seguridad perimetral:

| Capa | Mecanismo | Propósito |
|---|---|---|
| **1. Aplicativa** | Middleware `TenantMiddleware` + Filtro explícito `WHERE empresa_id = @Id` en queries críticas | Extrae `empresa_id` del JWT, lo inyecta en sesión Postgres, y evita fugas por queries manuales |
| **2. Base de Datos** | Políticas RLS (`ALTER TABLE ... ENABLE ROW LEVEL SECURITY`) | Bloqueo físico a nivel storage. Si alguien escapa el middleware o hace query directa, PG devuelve 0 filas |

## Criterios de Aceptación
- [ ] **CA-01:** El JWT embebido contiene obligatoriamente: `user_id`, `empresa_id`, `perfil_id`, `tipo_usuario`, `permisos[]`.
- [ ] **CA-02:** El middleware extrae `empresa_id` del claim JWT y ejecuta `SELECT set_config('app.current_empresa_id', @val, true)` antes de pasar al handler.
- [ ] **CA-03:** Las políticas RLS existentes en la migración base usan `current_setting('app.current_empresa_id', true)::UUID` en cláusulas `USING` y `WITH CHECK`.
- [ ] **CA-04:** El middleware es configurable: permite saltarlo para endpoints públicos, healthchecks y operaciones de Super Admin cuando aplique.
- [ ] **CA-05:** Cualquier intento de manipular `empresa_id` en el cuerpo de la petición REST retorna `403 Forbidden` sin alterar datos.

## Implementación Técnica Requerida

### 1. Middleware Core
```csharp
// src/Freiroute.API/Middleware/TenantMiddleware.cs
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger) => (_next, _logger) = (next, logger);

    public async Task InvokeAsync(HttpContext context, IDbConnection db)
    {
        var companyClaim = context.User?.FindFirst("empresa_id")?.Value;
        
        if (!string.IsNullOrEmpty(companyClaim))
        {
            await db.ExecuteAsync(
                "SELECT set_config('app.current_empresa_id', @Val, true)", 
                new { Val = companyClaim });
            _logger.LogDebug("Tenant injection applied: {EmpresaId}", companyClaim);
        }
        else
        {
            _logger.LogWarning("JWT missing 'empresa_id' claim. Request allowed but RLS may block.");
        }

        await _next(context);
    }
}
```

### 2. Registro en Pipeline (`Program.cs`)
```csharp
builder.Services.AddSingleton<IDbConnection>(sp => 
    new NpgsqlConnection(builder.Configuration.GetConnectionString("SupabaseConnection")));

// Orden crítico: Auth → TenantInjection → Authorization → MapControllers
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();
app.MapControllers();
```

## Matriz de Permisos vs Filtrado
| Rol | Filtro `empresa_id` | Acceso a otras empresas | Comportamiento |
|---|---|---|---|
| `SUPER_ADMIN` | `NO` | `SÍ` | Middleware detecta rol y salta `set_config` o aplica política RLS especial (`USING true`) |
| `ADMIN` | `SÍ` | `NO` | Filtro estricto aplicado por middleware + RLS |
| `OPERADOR/DISPATCHER` | `SÍ` | `NO` | Hereda filtro del token JWT generado por el Admin |

## Casos de Prueba Críticos (QA Security)
| Escenario | Acción | Resultado Esperado |
|---|---|---|
| IDOR Cross-Tenant | GET `/api/usuarios/{id_de_otro_tenant}` | `404 Not Found` (no revela existencia) |
| Payload Tampered | POST `/api/ordenes` con `empresa_id` manual en body | Ignorado. Usa `empresa_id` del JWT |
| Token Sin Claim | Llamada API sin `empresa_id` en JWT | Middleware registra warning. RLS bloquea lectura de tablas privadas |
| Super Admin Global | GET `/api/empresas` | Retorna lista completa. Ignora RLS de datos de negocio |

## Dependencias Técnicas
- Requiere configuración funcional de JWT (`JwtBearerOptions`) con mapeo correcto de claims.
- Requiere que todas las tablas nuevas incluyan `CREATE POLICY ... USING (empresa_id = (current_setting(...)))`.
- Debe documentarse en Swagger que el endpoint expone datos aislados.

---
