# ADR-004: Row Level Security para Aislamiento Multi-Tenant

| Campo | Valor |
|---|---|
| **ID** | ADR-004 |
| **Título** | Row Level Security (RLS) de PostgreSQL como mecanismo de aislamiento multi-tenant |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-20 |
| **Decidido por** | Arquitecto de software + DBA |
| **Revisado en** | Vittal Sprint 0 — Decisión crítica de seguridad |

---

## Contexto

El sistema SaaS es multi-tenant: múltiples organizaciones (tenants) comparten la misma base de datos. La pregunta crítica de arquitectura es: **¿cómo garantizar que los datos de un tenant nunca sean accesibles por otro tenant?**

Existen dos enfoques fundamentalmente distintos:

1. **Aislamiento en código**: el DAL filtra siempre por `tenant_id` en cada consulta
2. **Aislamiento en base de datos**: RLS de PostgreSQL impide físicamente el acceso cruzado de datos

El riesgo de una fuga de datos entre tenants en un sistema médico (HIPAA, datos sensibles) es inaceptable, tanto técnica como legalmente.

---

## Decisión

**Implementaremos RLS de PostgreSQL como segunda capa de seguridad**, además del filtro por `tenant_id` en el código DAL.

```sql
-- En cada tabla de negocio:
ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY;

CREATE POLICY "tenant_isolation" ON [tabla]
  FOR ALL
  USING (tenant_id = (current_setting('app.current_tenant_id', true))::UUID);
```

El filtro en código (DAL) sigue siendo obligatorio — RLS es la red de seguridad, no el mecanismo primario.

---

## Alternativas Evaluadas

### Opción A: Solo filtro en código DAL (RECHAZADA como única capa)

```csharp
// Cada consulta filtra por tenant_id
const string sql = "SELECT * FROM productos WHERE tenant_id = @TenantId AND activo = true";
return await _db.QueryAsync<Producto>(sql, new { TenantId = tenantId });
```

**Ventajas:**
- Simple — no requiere configurar RLS en PostgreSQL
- El código es autoexplicativo

**Desventajas que motivaron añadir RLS como segunda capa:**
- Un bug en el código (olvidar el filtro `WHERE tenant_id = @TenantId`) expone todos los datos
- Una consulta ad-hoc ejecutada directamente en la BD (por un DBA o herramienta de monitoreo) no tiene restricción
- EF Core con `IgnoreQueryFilters()` puede bypass el filtro global
- No hay defensa en profundidad — un solo punto de fallo

### Opción B: Bases de datos separadas por tenant (RECHAZADA)

**Ventajas:**
- Aislamiento físico total — imposible cruzar datos
- Backup y restore por tenant
- Cumplimiento regulatorio más simple (cada tenant tiene su propia instancia)

**Desventajas que motivaron su rechazo:**
- Costo: N bases de datos en Supabase = N proyectos pagados
- Complejidad operativa: migraciones deben aplicarse a N bases de datos simultáneamente
- Imposible con el modelo de Supabase (un proyecto = una BD)
- Inmanejable para > 20 tenants sin herramientas especializadas (Flyway, Liquibase)

### Opción C: Schemas separados por tenant en la misma BD (RECHAZADA)

```sql
-- Schema por tenant: mediccore.pacientes, clinicaXYZ.pacientes
CREATE SCHEMA mediccore;
CREATE TABLE mediccore.pacientes (...);
```

**Ventajas:**
- Aislamiento a nivel de schema en PostgreSQL
- Posible con la misma instancia de Supabase

**Desventajas que motivaron su rechazo:**
- Supabase CLI de migraciones no soporta múltiples schemas de tenant fácilmente
- PostgREST (API auto-generada de Supabase) no maneja bien el schema-per-tenant
- Las migraciones deben ejecutarse N veces (una por tenant) — complejidad enorme
- El número de schemas puede crecer indefinidamente

### Opción D: RLS como segunda capa de aislamiento (ELEGIDA) ✅

**Ventajas:**
- **Defensa en profundidad**: dos capas independientes de seguridad
- Un bug en el código DAL (olvidar el filtro) es neutralizado por RLS
- Las consultas directas a la BD (DBA, herramientas de monitoreo) también respetan el aislamiento
- `current_setting('app.current_tenant_id')` se establece automáticamente al inicio de cada sesión
- Cero overhead en el código de aplicación una vez configurado
- PostgREST de Supabase respeta RLS automáticamente (seguridad para la API BaaS también)

**Desventajas aceptadas:**
- Configuración inicial más compleja (RLS en cada tabla)
- El `current_setting` debe establecerse antes de cada query (middleware del DAL)
- Las consultas de Super Admin (que necesitan ver todos los tenants) requieren `SET ROLE postgres` o deshabilitar RLS temporalmente
- Debugging más complejo: si la consulta retorna vacío, puede ser RLS o puede ser que no hay datos

---

## Implementación

### En la migración SQL

```sql
-- 1. Habilitar RLS en la tabla
ALTER TABLE productos ENABLE ROW LEVEL SECURITY;

-- 2. Política para operaciones del usuario regular (tenant aislado)
CREATE POLICY "tenant_isolation" ON productos
  FOR ALL
  USING (tenant_id = (current_setting('app.current_tenant_id', true))::UUID);

-- 3. Política para Super Admin (puede ver todos los tenants)
CREATE POLICY "super_admin_access" ON productos
  FOR ALL
  TO service_role  -- rol de Supabase con privilegios elevados
  USING (true);
```

### En el middleware del API

```csharp
// Se ejecuta antes de cada request autenticado
public async Task InvokeAsync(HttpContext context, IDbConnection db)
{
    var tenantId = context.User.GetTenantId();
    if (tenantId != Guid.Empty)
    {
        await db.ExecuteAsync(
            "SELECT set_config('app.current_tenant_id', @TenantId, true)",
            new { TenantId = tenantId.ToString() });
    }
    await _next(context);
}
```

---

## Consecuencias

### Positivas
- Dos capas de seguridad independientes para el aislamiento multi-tenant
- Un bug en el código NO puede provocar una fuga de datos entre tenants
- El cumplimiento regulatorio (datos médicos, GDPR, HIPAA) es más robusto
- PostgREST (BaaS API de Supabase) hereda el aislamiento automáticamente

### Negativas / Trade-offs aceptados
- Toda nueva tabla de negocio requiere `ENABLE ROW LEVEL SECURITY` y la política de aislamiento
- Las operaciones de mantenimiento de BD deben usar el `service_role` de Supabase
- El Super Admin del sistema requiere acceso especial (bypassar RLS con `service_role`)
- El middleware de establecer `current_setting` agrega una query extra por request

### Criterio de revisión
Si el sistema evoluciona a requerir Schemas separados por tenant por motivos regulatorios (por ejemplo, datos de salud en diferentes jurisdicciones geográficas), revisar este ADR para considerar la arquitectura de schema-per-tenant.

---

## Referencias

- [PostgreSQL Row Level Security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)
- [Supabase — RLS Guide](https://supabase.com/docs/guides/auth/row-level-security)
- [Multi-tenant SaaS con PostgreSQL RLS](https://www.citusdata.com/blog/2016/08/10/sharding-for-multi-tenant-apps/)
- ADR-001 — Dapper como ORM (la segunda capa de filtro está en el DAL)
- ADR-003 — Supabase como BaaS (RLS es nativo de PostgreSQL/Supabase)
