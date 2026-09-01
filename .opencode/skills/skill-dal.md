# Skill: @IngenieroDatos (Ingeniero de Datos Freiroute TMS)

## Rol
**@IngenieroDatos** es responsable de toda la capa de datos: migraciones SQL versionadas con Supabase CLI, RLS, triggers, repositorios Dapper, y el aislamiento multi-tenant en todas las operaciones de base de datos. Actúa inmediatamente después de @Arquitecto.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md completo
2. Leer spec.md del módulo (docs/specs/HU-XXX-nombre.md)
3. Revisar la Entity y las interfaces definidas por @Arquitecto
4. Revisar migraciones existentes para evitar conflictos de nombres
```

### 2. Migraciones SQL (Supabase CLI)

**Crear migración:**
```bash
supabase migration new [nombre_descriptivo_en_snake_case]
# Ejemplo: supabase migration new crear_tabla_embarques
# Genera: supabase/migrations/20260101120000_crear_tabla_embarques.sql
```

**Estructura obligatoria de toda migración de negocio:**
```sql
-- ============================================================
-- Migración: [nombre descriptivo]
-- Módulo: [nombre del módulo TMS]
-- HU: HU-XXX
-- Fecha: YYYY-MM-DD
-- ============================================================

-- Tabla principal
CREATE TABLE IF NOT EXISTS [tabla] (
    -- ── Campos base obligatorios (NO modificar) ───────────────
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id          UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    activo              BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ,

    -- ── Campos de negocio del módulo ──────────────────────────
    nombre              VARCHAR(200) NOT NULL,
    -- ... campos específicos del módulo

    -- ── Constraints ───────────────────────────────────────────
    CONSTRAINT [tabla]_empresa_id_fk FOREIGN KEY (empresa_id) REFERENCES empresas(id)
);

-- Comentarios de tabla y columnas (en español)
COMMENT ON TABLE [tabla] IS '[Descripción de la tabla en español]';
COMMENT ON COLUMN [tabla].id IS 'Identificador único del registro';
COMMENT ON COLUMN [tabla].empresa_id IS 'Empresa dueña del registro (discriminador multi-tenant)';
COMMENT ON COLUMN [tabla].activo IS 'Indica si el registro está activo (false = eliminado lógicamente)';
COMMENT ON COLUMN [tabla].nombre IS '[Descripción del campo]';

-- Índices obligatorios
CREATE INDEX idx_[tabla]_empresa_id ON [tabla](empresa_id);
CREATE INDEX idx_[tabla]_activo ON [tabla](activo);
CREATE INDEX idx_[tabla]_empresa_activo ON [tabla](empresa_id, activo);
-- Índices adicionales por campos de búsqueda frecuente
-- CREATE INDEX idx_[tabla]_estado ON [tabla](estado) WHERE activo = true;

-- Row Level Security
ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation_[tabla]" ON [tabla]
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

-- Trigger de fecha_modificacion
CREATE TRIGGER trg_[tabla]_fecha_modificacion
    BEFORE UPDATE ON [tabla]
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

### 3. Función Global (crear solo una vez, en migración base)

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_funciones_base.sql

-- Función reutilizable para actualizar fecha_modificacion
CREATE OR REPLACE FUNCTION update_fecha_modificacion()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION update_fecha_modificacion() IS 
'Actualiza automáticamente fecha_modificacion antes de cada UPDATE';
```

### 4. Tabla de Auditoría (migración base)

```sql
CREATE TABLE IF NOT EXISTS auditoria_actividad (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID REFERENCES empresas(id) ON DELETE SET NULL,
    usuario_id      UUID,
    modulo          VARCHAR(100) NOT NULL,
    accion          VARCHAR(50) NOT NULL,     -- CREATE, UPDATE, DEACTIVATE, LOGIN, etc.
    entidad_tipo    VARCHAR(100),             -- nombre de la tabla afectada
    entidad_id      UUID,                     -- ID del registro afectado
    ip_address      INET,
    user_agent      TEXT,
    detalles        JSONB,                    -- datos adicionales en formato JSON
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE auditoria_actividad IS 'Log de auditoría de todas las acciones del sistema';

CREATE INDEX idx_auditoria_empresa_id ON auditoria_actividad(empresa_id);
CREATE INDEX idx_auditoria_usuario_id ON auditoria_actividad(usuario_id);
CREATE INDEX idx_auditoria_fecha ON auditoria_actividad(fecha_creacion DESC);
CREATE INDEX idx_auditoria_modulo ON auditoria_actividad(modulo);

ALTER TABLE auditoria_actividad ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_auditoria" ON auditoria_actividad
    FOR SELECT
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);
```

### 5. Repositorios DAL (Dapper)

**Implementación estándar:**
```csharp
// Freiroute.DAL/Repositories/[Modulo]Repository.cs
namespace Freiroute.DAL.Repositories;

public class [Modulo]Repository : I[Modulo]Repository
{
    private readonly IDbConnection _db;

    public [Modulo]Repository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<IEnumerable<[Modulo]>> GetAllAsync(Guid empresaId)
    {
        const string sql = @"
            SELECT
                id              AS Id,
                empresa_id      AS EmpresaId,
                nombre          AS Nombre,
                activo          AS Activo,
                fecha_creacion  AS FechaCreacion,
                fecha_modificacion AS FechaModificacion
            FROM [tabla]
            WHERE empresa_id = @EmpresaId
              AND activo = true
            ORDER BY nombre ASC";

        return await _db.QueryAsync<[Modulo]>(sql, new { EmpresaId = empresaId });
    }

    public async Task<[Modulo]?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            SELECT
                id, empresa_id, nombre, activo,
                fecha_creacion, fecha_modificacion
            FROM [tabla]
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true";

        return await _db.QueryFirstOrDefaultAsync<[Modulo]>(sql,
            new { Id = id, EmpresaId = empresaId });
    }

    public async Task<Guid> CreateAsync([Modulo] entidad)
    {
        const string sql = @"
            INSERT INTO [tabla] (
                empresa_id, nombre,
                activo, fecha_creacion
            )
            VALUES (
                @EmpresaId, @Nombre,
                true, NOW()
            )
            RETURNING id";

        return await _db.ExecuteScalarAsync<Guid>(sql, entidad);
    }

    public async Task<bool> UpdateAsync([Modulo] entidad)
    {
        const string sql = @"
            UPDATE [tabla]
            SET
                nombre             = @Nombre,
                fecha_modificacion = NOW()
            WHERE id          = @Id
              AND empresa_id  = @EmpresaId
              AND activo      = true";

        var filas = await _db.ExecuteAsync(sql, entidad);
        return filas > 0;
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        const string sql = @"
            UPDATE [tabla]
            SET
                activo             = false,
                fecha_modificacion = NOW()
            WHERE id         = @Id
              AND empresa_id = @EmpresaId
              AND activo     = true";

        var filas = await _db.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return filas > 0;
    }
}
```

**Reglas críticas de Dapper para Freiroute:**
1. SIEMPRE mapear alias SQL → nombre de propiedad C# (PascalCase → snake_case con AS)
2. SIEMPRE filtrar `AND empresa_id = @EmpresaId` Y `AND activo = true` en SELECT
3. SIEMPRE filtrar `AND empresa_id = @EmpresaId` en UPDATE y DEACTIVATE
4. NUNCA usar `DELETE FROM` — solo `UPDATE ... SET activo = false`
5. NUNCA usar `SELECT *` — listar columnas explícitamente
6. Usar `RETURNING id` en INSERT para obtener el UUID generado por la BD

### 6. Queries Específicas del Dominio TMS

**Embarques con joins:**
```sql
SELECT
    e.id                        AS Id,
    e.numero_embarque           AS NumeroEmbarque,
    e.estado                    AS Estado,
    o.nombre                    AS OrigenNombre,
    d.nombre                    AS DestinoNombre,
    c.nombre                    AS CarrierNombre,
    co.nombre_completo          AS ConductorNombre,
    e.fecha_pickup_planificada  AS FechaPickupPlanificada,
    e.fecha_entrega_requerida   AS FechaEntregaRequerida,
    e.eta                       AS Eta,
    e.peso_total                AS PesoTotal,
    e.costo_flete               AS CostoFlete,
    e.activo                    AS Activo,
    e.fecha_creacion            AS FechaCreacion
FROM embarques e
    INNER JOIN ubicaciones o  ON e.origen_id  = o.id
    INNER JOIN ubicaciones d  ON e.destino_id = d.id
    LEFT  JOIN carriers c     ON e.carrier_id = c.id
    LEFT  JOIN conductores co ON e.conductor_id = co.id
WHERE e.empresa_id = @EmpresaId
  AND e.activo     = true
ORDER BY e.fecha_creacion DESC
LIMIT @PageSize OFFSET @Offset;
```

**Track & Trace — última posición por embarque:**
```sql
SELECT DISTINCT ON (embarque_id)
    embarque_id,
    latitud,
    longitud,
    velocidad,
    fecha_registro
FROM posiciones_gps
WHERE empresa_id = @EmpresaId
ORDER BY embarque_id, fecha_registro DESC;
```

### 7. Paginación Estándar

```csharp
// Freiroute.Utility/Pagination/PagedQuery.cs
public class PagedQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;   // RNF-01.4: 20 registros/página
    public int Offset => (PageNumber - 1) * PageSize;
}

// Usar en repositorios:
const string sql = @"
    SELECT ... FROM [tabla]
    WHERE empresa_id = @EmpresaId AND activo = true
    ORDER BY fecha_creacion DESC
    LIMIT @PageSize OFFSET @Offset";

return await _db.QueryAsync<[Modulo]>(sql, new
{
    EmpresaId = empresaId,
    query.PageSize,
    query.Offset
});
```

### 8. Inyección de Dependencias

```csharp
// Freiroute.IOC/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddFreirouteServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Conexión a base de datos (Supabase/PostgreSQL)
        services.AddScoped<IDbConnection>(_ =>
            new NpgsqlConnection(
                configuration.GetConnectionString("SupabaseConnection")));

        // Repositorios DAL
        services.AddScoped<I[Modulo]Repository, [Modulo]Repository>();

        // Servicios BLL
        services.AddScoped<I[Modulo]Service, [Modulo]Service>();

        // Validators
        services.AddScoped<IValidator<[Modulo]RequestDto>, [Modulo]Validator>();

        return services;
    }
}
```

### 9. Configuración de Conexión

```json
// appsettings.json
{
  "ConnectionStrings": {
    "SupabaseConnection": "Host=127.0.0.1;Port=54322;Database=postgres;Username=postgres;Password=postgres"
  }
}

// appsettings.Production.json — leer desde variables de entorno
{
  "ConnectionStrings": {
    "SupabaseConnection": ""
  }
}
```

```bash
# Comandos Supabase CLI
supabase start                    # Levantar instancia local
supabase migration new [nombre]   # Crear nueva migración
supabase db push                  # Aplicar migraciones pendientes
supabase db diff                  # Ver cambios no aplicados
supabase db reset                 # Reiniciar BD local con todas las migraciones
```

### 10. Middleware para empresa_id en PostgreSQL

```csharp
// Freiroute.API/Middleware/TenantMiddleware.cs
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IDbConnection db)
    {
        var empresaIdClaim = context.User?.FindFirst("empresa_id")?.Value;

        if (!string.IsNullOrEmpty(empresaIdClaim))
        {
            // Inyectar empresa_id en la sesión de PostgreSQL para que RLS funcione
            await db.ExecuteAsync(
                "SELECT set_config('app.current_empresa_id', @EmpresaId, true)",
                new { EmpresaId = empresaIdClaim });
        }

        await _next(context);
    }
}
```

### 11. Checklist de Migración por Módulo

- [ ] Migración creada con `supabase migration new [nombre]`
- [ ] Tabla incluye: `id`, `empresa_id`, `activo`, `fecha_creacion`, `fecha_modificacion`
- [ ] FK a `empresas(id)` con `ON DELETE RESTRICT`
- [ ] Índices: `idx_[tabla]_empresa_id`, `idx_[tabla]_activo`, `idx_[tabla]_empresa_activo`
- [ ] RLS habilitado: `ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY`
- [ ] Policy de aislamiento: `CREATE POLICY "empresa_isolation_[tabla]"`
- [ ] Trigger `trg_[tabla]_fecha_modificacion` creado
- [ ] Comentarios en español en tabla y todas las columnas
- [ ] Repositorio DAL implementado con alias SQL → PascalCase C#
- [ ] Todos los métodos filtran `empresa_id` AND `activo = true`
- [ ] `supabase db push` aplicado sin errores
- [ ] `supabase db diff` sin cambios pendientes

---

## Contexto Freiroute TMS

@IngenieroDatos asegura que la capa de datos del TMS soporte las operaciones críticas de transporte: órdenes, embarques, rutas, tracking GPS, documentos y facturación, siempre con aislamiento total por `empresa_id`. Las tablas del MVP incluyen: `empresas`, `usuarios`, `perfiles`, `permisos`, `ubicaciones`, `clientes`, `carriers`, `vehiculos`, `conductores`, `ordenes`, `embarques`, `paradas_embarque`, `posiciones_gps`, `documentos`, `auditoria_actividad`.
