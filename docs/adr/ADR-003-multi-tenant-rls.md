# ADR-003: Multi-Tenant con empresa_id + Row Level Security

## Estado
Aceptado

## Fecha
2026

## Contexto
Freiroute es un SaaS donde múltiples empresas de transporte (tenants) comparten la misma instancia de base de datos. Es crítico que los datos de una empresa nunca sean visibles ni accesibles desde otra empresa, incluso en caso de error de programación en la capa de aplicación.

## Decisión
El aislamiento multi-tenant se implementa con **dos capas complementarias**:
1. **Capa de aplicación:** Toda query SQL incluye `WHERE empresa_id = @EmpresaId`
2. **Capa de base de datos:** RLS (Row Level Security) de PostgreSQL como red de seguridad

## Implementación

### Capa 1 — empresa_id en código C#

Todo método de repositorio recibe `Guid empresaId` como parámetro y lo incluye en la cláusula WHERE:

```sql
-- Ejemplo obligatorio en TODO GetAll
SELECT * FROM embarques
WHERE empresa_id = @EmpresaId AND activo = true
```

El `empresa_id` se extrae siempre del JWT del usuario autenticado, nunca del body del request.

### Capa 2 — RLS en PostgreSQL

```sql
-- En TODA tabla de negocio
ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY;

CREATE POLICY "empresa_isolation" ON [tabla]
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);
```

El `TenantMiddleware` inyecta el `empresa_id` del JWT en la sesión de PostgreSQL:

```sql
SELECT set_config('app.current_empresa_id', '{empresa_id}', true);
```

### Estructura del JWT

```json
{
  "user_id":      "uuid",
  "empresa_id":   "uuid",
  "perfil_id":    "uuid",
  "tipo_usuario": "SUPER_ADMIN | ADMIN | OPERADOR | DISPATCHER | CONDUCTOR | CLIENTE",
  "permisos":     ["embarques:read", "ordenes:create", "carriers:update"],
  "nombre":       "Juan Pérez",
  "exp":          1234567890
}
```

### Regla de oro

> **El código nunca confía solo en RLS.** RLS es la última línea de defensa. La primera línea es siempre el filtro `empresa_id` explícito en el código C#.

## Alternativas Consideradas

1. **Schema separado por tenant** — Descartada porque Supabase no facilita la creación dinámica de schemas y complica las migraciones.
2. **Base de datos separada por tenant** — Descartada por costo operacional excesivo en la fase inicial.
3. **Solo empresa_id en código, sin RLS** — Descartada porque un solo error de programación podría exponer datos de otro tenant. RLS es la red de seguridad obligatoria.

## Consecuencias

**Positivas:**
- Doble capa de protección — un bug en el código no expone datos de otro tenant
- RLS funciona incluso para consultas directas a la BD (útil para reportes externos)
- El sistema es auditable: cada registro tiene su `empresa_id` siempre visible

**Negativas / Trade-offs:**
- El `TenantMiddleware` debe ejecutarse antes de cualquier operación de BD
- Todas las queries tienen el overhead de `WHERE empresa_id = @EmpresaId`
- Los tests deben siempre incluir `empresaId` como parámetro

## Módulos Afectados
Todos los módulos con tablas de negocio (EP-01 al EP-20).
Excepción: tabla `empresas` (no tiene `empresa_id` propio — es la tabla raíz de tenants).
