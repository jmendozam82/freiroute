# ADR-007: JWT Claims Structure y middleware de inyección tenant

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
El sistema requiere autenticación sin estado que proporcione información completa del usuario, su contexto de empresa (tenant), perfil de rol y permisos disponibles en cada request HTTP. La elección entre cookies tradicionales vs JWT impacta escalabilidad horizontal, consumo de memoria en servidores, compatibilidad con SPAs externas y performance de validación por request. Se necesita también inyectar el `empresa_id` en la sesión PostgreSQL para activar RLS automáticamente antes de ejecutar cualquier query.

## Decisión
El sistema implementará JWT como mecanismo de autenticación con la siguiente estructura de claims obligatorios:

```json
{
    "user_id": "uuid-del-usuario",
    "empresa_id": "uuid-de-la-empresa-tenant",
    "perfil_id": "uuid-del-perfil-asignado",
    "tipo_usuario": "SUPER_ADMIN | ADMIN | OPERADOR | CONDUCTOR | CLIENTE",
    "permisos": ["empresas:read", "ordenes:create", "embarques:update"],
    "exp": 1234567890,
    "iat": 1234567000
}
```

### Middleware de inyección obligatorio
```csharp
// TenantMiddleware.cs — ORDEN CRÍTICO: debe ejecutarse DESPUÉS de Auth, ANTES de Authorization
app.UseAuthentication();           // 1. Valida token JWT
app.UseMiddleware<TenantMiddleware>();  // 2. Inyecta empresa_id → set_config()
app.UseAuthorization();            // 3. Verifica políticas de permisos
app.MapControllers();
```

```sql
-- PostgreSQL session variable para RLS
SELECT set_config('app.current_empresa_id', @Val, true);
-- La policy usa: WHERE empresa_id = current_setting('app.current_empresa_id')::UUID
```

Justificación principal: JWT permite escalar horizontalmente sin state server ni Redis para sesiones. Los claims transportan toda la información de autorización necesaria sin consultar BD en cada request. El middleware inyecta `empresa_id` en sesión PostgreSQL para activar RLS transparentemente antes de cualquier query Dapper.

## Alternativas Consideradas
1. **Cookies HttpOnly con servidor state** — Demasiada complejidad operativa (Redis cluster para sessions). Los tokens JWT son portables hacia APIs externas (portales de carriers, clientes enterprise) sin configuración adicional.
2. **OAuth2 authorization code flow completo** — Overkill para MVP internal dashboard. Solo se implementará OAuth2 (Google/Microsoft SSO) en fase avanzada EP-01 cuando sea explícitamente requerido por clientes enterprise.
3. **API Keys estáticas** — Insuficiente granularity para RBAC multi-tenant donde múltiples usuarios comparten same empresa pero tienen diferentes roles y accesos modulares.

## Consecuencias
**Positivas:**
- Autenticación stateless scale-out horizontal nativo
- Permisos decodificables inmediatamente sin consultas BD adicionales
- Compatibilidad nativa con Supabase Auth (produce JWTs compatibles)
- Token único contiene toda la información de identidad y autorización

**Negativas / Trade-offs:**
- Tokens no revocables individualmente fácilmente (necesita blacklist table o refresh tokens cortos)
- Tamaño del payload aumenta ligeramente vs simple user_id (mitigable comprimiendo claims)
- Refresh token lifecycle debe gestionarse cuidadosamente (vencimiento configurable default 8h access, 30d refresh)
- Validación JWT consume CPU por sign/verify en cada request (aceptable: ~0.2ms/token)

## Módulos Afectados
Todo el pipeline HTTP de la aplicación. Este ADR afecta directamente a `TenantMiddleware`, `JwtBearerOptions` configuracion en Program.cs, controllers `[Authorize]` atributos, y cualquier endpoint que necesite decodificar claims desde `User.Claims`. Sin este ADR aplicado, HU-002 (RLS + aislamiento multi-tenant) NO puede funcionar.

---
