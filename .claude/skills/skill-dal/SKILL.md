---
description: Patrón repositorio Dapper y políticas RLS para Freiroute TMS. Úsalo para crear migraciones SQL con Supabase CLI, implementar Row Level Security, desarrollar repositorios DAL con Dapper que filtren por empresa_id, y crear triggers y funciones PostgreSQL.
---

# Skill: DAL — Repositorios Dapper y Migraciones SQL Freiroute TMS

## Estructura de Migración Estándar

```bash
# Crear migración (SIEMPRE con Supabase CLI)
supabase migration new crear_tabla_[nombre]
```

```sql
-- supabase/migrations/[timestamp]_crear_tabla_[nombre].sql
-- ============================================================
-- Migración: Crear tabla [nombre]
-- Módulo: [nombre del módulo TMS]
-- Descripción: [descripción en español]
-- ============================================================

-- Tabla principal
CREATE TABLE IF NOT EXISTS [nombre] (
    id                  UUID        NOT NULL DEFAULT gen_random_uuid(),
    empresa_id          UUID        NOT NULL,
    -- ── Campos de negocio ──────────────────────────────────────
    nombre              VARCHAR(200) NOT NULL,
    descripcion         TEXT,
    -- ── Campos de auditoría (OBLIGATORIOS en toda tabla) ──────
    activo              BOOLEAN     NOT NULL DEFAULT true,
    fecha_creacion      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_modificacion  TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_[nombre]         PRIMARY KEY (id),
    CONSTRAINT fk_[nombre]_empresa FOREIGN KEY (empresa_id)
        REFERENCES empresas(id) ON DELETE RESTRICT
);

-- Comentarios de tabla (en español)
COMMENT ON TABLE [nombre] IS '[Descripción del módulo TMS]';
COMMENT ON COLUMN [nombre].empresa_id IS 'Discriminador de tenant multi-empresa';
COMMENT ON COLUMN [nombre].activo IS 'Soft delete: false = registro inactivo (NUNCA eliminar físicamente)';

-- ── Índices obligatorios ───────────────────────────────────────────────
CREATE INDEX idx_[nombre]_empresa_id ON [nombre](empresa_id);
CREATE INDEX idx_[nombre]_activo     ON [nombre](activo) WHERE activo = true;

-- ── Trigger de fecha_modificacion (OBLIGATORIO en toda tabla) ─────────
CREATE TRIGGER trg_[nombre]_fecha_modificacion
    BEFORE UPDATE ON [nombre]
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();

-- ── Row Level Security (OBLIGATORIO en tablas de negocio) ─────────────
ALTER TABLE [nombre] ENABLE ROW LEVEL SECURITY;

CREATE POLICY pol_[nombre]_select ON [nombre]
    FOR SELECT USING (
        empresa_id = (current_setting('app.current_empresa_id'))::UUID
    );

CREATE POLICY pol_[nombre]_insert ON [nombre]
    FOR INSERT WITH CHECK (
        empresa_id = (current_setting('app.current_empresa_id'))::UUID
    );

CREATE POLICY pol_[nombre]_update ON [nombre]
    FOR UPDATE USING (
        empresa_id = (current_setting('app.current_empresa_id'))::UUID
        AND activo = true
    );

-- NOTA: SIN política DELETE — no existe DELETE físico en el sistema
```

## Patrón Repositorio Dapper

```csharp
// Freiroute.DAL/Repositories/[Modulo]Repository.cs
namespace Freiroute.DAL.Repositories;

public class [Modulo]Repository : I[Modulo]Repository
{
    private readonly IDbConnection _db;
    private readonly ILogger<[Modulo]Repository> _logger;

    public [Modulo]Repository(IDbConnection db, ILogger<[Modulo]Repository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<[Modulo]ResponseDto>> GetAllAsync(Guid empresaId)
    {
        const string sql = """
            SELECT
                id,
                empresa_id,
                nombre,
                -- ... campos del módulo
                activo,
                fecha_creacion,
                fecha_modificacion
            FROM [tabla]
            WHERE empresa_id = @EmpresaId  -- SIEMPRE filtrar por tenant
              AND activo = true
            ORDER BY fecha_creacion DESC
            """;

        return await _db.QueryAsync<[Modulo]ResponseDto>(sql, new { EmpresaId = empresaId });
    }

    public async Task<[Modulo]ResponseDto?> GetByIdAsync(Guid id, Guid empresaId)
    {
        const string sql = """
            SELECT id, empresa_id, nombre, activo, fecha_creacion, fecha_modificacion
            FROM [tabla]
            WHERE id = @Id
              AND empresa_id = @EmpresaId  -- SIEMPRE incluir empresa_id
              AND activo = true
            """;

        return await _db.QueryFirstOrDefaultAsync<[Modulo]ResponseDto>(
            sql, new { Id = id, EmpresaId = empresaId });
    }

    public async Task<Guid> CreateAsync([Modulo]RequestDto dto, Guid empresaId)
    {
        const string sql = """
            INSERT INTO [tabla] (empresa_id, nombre, activo)
            VALUES (@EmpresaId, @Nombre, true)
            RETURNING id
            """;

        return await _db.ExecuteScalarAsync<Guid>(sql, new
        {
            EmpresaId = empresaId,
            dto.Nombre
        });
    }

    public async Task<bool> UpdateAsync(Guid id, [Modulo]RequestDto dto, Guid empresaId)
    {
        const string sql = """
            UPDATE [tabla]
            SET nombre = @Nombre
                -- fecha_modificacion la actualiza el trigger automáticamente
            WHERE id = @Id
              AND empresa_id = @EmpresaId  -- Aislamiento de tenant
              AND activo = true
            """;

        var rows = await _db.ExecuteAsync(sql, new
        {
            Id = id,
            EmpresaId = empresaId,
            dto.Nombre
        });
        return rows > 0;
    }

    public async Task<bool> DeactivateAsync(Guid id, Guid empresaId)
    {
        // NUNCA DELETE — siempre soft delete
        const string sql = """
            UPDATE [tabla]
            SET activo = false
            WHERE id = @Id
              AND empresa_id = @EmpresaId
              AND activo = true
            """;

        var rows = await _db.ExecuteAsync(sql, new { Id = id, EmpresaId = empresaId });
        return rows > 0;
    }
}
```

## Registro en IOC (`Freiroute.IOC/`)

```csharp
// Freiroute.IOC/DependencyInjection.cs
services.AddScoped<I[Modulo]Repository, [Modulo]Repository>();
services.AddScoped<I[Modulo]Service, [Modulo]Service>();
```

## Función Reutilizable update_fecha_modificacion

Esta función ya existe en la BD de Freiroute. Si no existe, crearla:

```sql
-- Solo crear si no existe (normalmente ya está en la BD)
CREATE OR REPLACE FUNCTION update_fecha_modificacion()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

## Comandos Supabase CLI

```bash
# Crear nueva migración
supabase migration new [nombre_descriptivo]

# Aplicar en base de datos local
supabase db push

# Ver estado de migraciones
supabase migration list

# Ver diff de BD vs migraciones
supabase db diff

# Iniciar BD local
supabase start

# Ver logs de BD local
supabase db logs
```

## Reglas de Oro DAL

1. ✅ TODA query filtra por `empresa_id` — incluso con RLS activo
2. ✅ TODA tabla nueva tiene índices `idx_[tabla]_empresa_id` e `idx_[tabla]_activo`
3. ✅ TODA tabla nueva tiene trigger `update_fecha_modificacion()`
4. ✅ TODA tabla nueva tiene RLS habilitado con políticas SELECT, INSERT, UPDATE
5. ✅ UUIDs generados con `gen_random_uuid()` en BD — nunca en C#
6. ❌ NUNCA DELETE físico — solo `UPDATE activo = false`
7. ❌ NUNCA SQL ad-hoc en producción — siempre `supabase migration new`
8. ❌ NUNCA datos de Tenant A accesibles por Tenant B
