# ADR-003: Multi-tenant con `empresa_id` discriminador + RLS PostgreSQL

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
Freiroute TMS es un SaaS multi-tenant donde cada empresa de transporte (tenant) necesita datos completamente aislados sin posibilidad de filtración entre organizaciones. Se requiere garantizar que un usuario del Tenant-A nunca pueda leer, modificar o incluso descubrir la existencia de registros del Tenant-B. Las opciones técnicas abarcan desde esquemas separados por tenant hasta shared-schema con filtros manuales o RLS a nivel base de datos.

## Decisión
El sistema implementará **doble capa de aislamiento**:

### Capa 1 — Discriminador en modelo (`empresa_id`)
```sql
-- Toda tabla de negocio contendrá esta columna obligatoria
empresa_id UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT
```

Reglas:
- **Toda consulta SQL DEBERÁ** incluir siempre `WHERE empresa_id = @EmpresaId AND activo = true`
- **NUNCA** se aceptará `empresa_id` desde el cuerpo del request REST — siempre proviene del JWT claim
- **Super Admin** tiene visibilidad global (ignora el filtro `empresa_id`)
- **Admin de tenant** y roles operativos están estrictamente filtrados por `empresa_id`

### Capa 2 — Row Level Security (RLS) en PostgreSQL
```sql
-- Habilitar RLS en cada tabla de negocio
ALTER TABLE [tabla] ENABLE ROW LEVEL SECURITY;

-- Política de aislamiento por tenant
CREATE POLICY "empresa_isolation_[tabla]" ON [tabla]
    FOR ALL
    USING (
        empresa_id = (current_setting('app.current_empresa_id', true))::UUID
    );
```

### Middleware inyección
```csharp
// TenantMiddleware.cs — Inyecta empresa_id antes de Authorization
await db.ExecuteAsync(
    "SELECT set_config('app.current_empresa_id', @Val, true)",
    new { Val = companyClaim });
```

Justificación principal: Defender en profundidad (Defense in Depth). Aunque los filtros manuales prevengan fugas, RLS garantiza que incluso una query directa sobre la BD o un error humano retornará 0 filas para tenants no autorizados. Esto es innegociable para compliance GDPR/SOC2.

## Alternativas Consideradas
1. **Esquemas separados por tenant** (`schema_tenant_a`, `schema_tenant_b`) — Descartado porque multiplica infraestructura, complica backups masivos, migraiones y reporting cross-tenant imposible.
2. **Filtro manual exclusivo** — Demasiado frágil. Un solo developer que olvide añadir `AND empresa_id` expone TODOS los datos de todos los tenants.
3. **TenantID en JWT vs Base de Datos** — El tenant_id debe estar SIEMPRE en ambas capas: JWT para autenticación/autorización y tabla `empresas` para referencia relacional.

## Consecuencias
**Positivas:**
- Aislamiento absoluto garantizado por dos mecanismos independientes
- Super Admin puede operar transversalmente sobre cualquier tenant
- Cumple requisitos regulatorios SOC2, ISO 27001, HIPAA (si aplica)
- Zero overhead computacional en RLS policies simples con UUID comparison

**Negativas / Trade-offs:**
- Cada nuevo desarrollador DEBE recordar incluir `empresa_id` en cada query manual Dapper
- Testing de integración requiere fixtures multi-tenant (varias empresas simuladas)
- Migraciones compartidas: todos los tenants comparten el mismo schema
- Debugging más complejo: ¿el problema es mi filtro o la policy RLS?

## Módulos Afectados
Todos los módulos del MVP (EP-01 al EP-20). Este es el mecanismo fundamental de seguridad perimetral. Sin este ADR, ningún otro módulo puede ser considerado seguro para producción.

---
