# ADR-008: Autenticación Stateless (JWT) con Claims de Tenant

| Campo | Valor |
|---|---|
| **ID** | ADR-008 |
| **Título** | Uso de JWT con custom claims (`tenant_id`, permisos) en lugar de sesiones en memoria/servidor |
| **Estado** | ✅ Aceptado |
| **Fecha** | 2026-01-25 |
| **Decidido por** | Arquitecto de software |
| **Revisado en** | Vittal Sprint 0 |

---

## Contexto

El sistema expone una API REST (BaaS) que debe ser consumida tanto por el Frontend web (MVC/Razor o SPA futuro) como por aplicaciones de terceros. La API requiere un mecanismo para autenticar a los usuarios y, críticamente, identificar a qué tenant pertenecen para asegurar el aislamiento de datos.

Las opciones para manejar el estado de autenticación son:
1. **Stateful:** Sesiones en el servidor (Cookies de sesión en memoria o Redis).
2. **Stateless:** JSON Web Tokens (JWT) pasados en el header de cada request.

Además, si se usa JWT, hay que decidir cuánta información (claims) incluir en el payload del token vs. consultarla en la base de datos en cada request.

---

## Decisión

**Implementaremos autenticación stateless utilizando JWT (generados por Supabase Auth) y extenderemos el payload del token con claims personalizados para el dominio: `tenant_id`, `perfil_id`, y un arreglo de `permisos[]`.**

---

## Alternativas Evaluadas

### Opción A: Sesiones en servidor (Stateful) (RECHAZADA)

**Ventajas:**
- Mayor seguridad teórica (fácil de invalidar la sesión instantáneamente).
- El cliente no tiene acceso al contenido de la sesión (se almacena en el servidor).
- No hay problemas de tamaño de payload (la cookie solo lleva un Session ID).

**Desventajas que motivaron su rechazo:**
- Escala mal horizontalmente: requiere sticky sessions o una caché distribuida (ej. Redis), lo que aumenta la complejidad y los costos de infraestructura.
- No es un estándar amigable para integraciones de terceros (BaaS API).
- Obliga a acoplar la capa de presentación con el backend si se usan cookies HttpOnly en arquitecturas no-monolíticas.

### Opción B: JWT básico (solo User ID) (RECHAZADA)

El token solo contiene el `sub` (User ID). En cada request, el API consulta la base de datos para obtener el `tenant_id` y los permisos del usuario antes de procesar la solicitud.

**Ventajas:**
- Token pequeño.
- Si cambian los permisos o el tenant del usuario, el cambio es efectivo inmediatamente en el próximo request.

**Desventajas que motivaron su rechazo:**
- Impacto severo en el rendimiento: TODO request a la API (incluso una simple consulta de catálogo) requiere una consulta previa a la base de datos solo para averiguar los permisos y el tenant. En un sistema altamente transaccional, esto sobrecarga la base de datos innecesariamente.

### Opción C: JWT "Gordo" con Claims de Dominio (ELEGIDA) ✅

El payload del token JWT incluye la identidad y el contexto de autorización completo:
```json
{
  "sub": "user_id_uuid",
  "app_metadata": {
    "tenant_id": "tenant_uuid",
    "perfil_id": "perfil_uuid",
    "permisos": ["pacientes:read", "pacientes:create", "citas:read", "citas:update"]
  },
  "exp": 1711234567
}
```

**Ventajas:**
- **Rendimiento:** El middleware de autorización del API puede validar el acceso (roles/permisos) y extraer el `tenant_id` en tiempo de CPU (0 ms), sin tocar la base de datos.
- **Escalabilidad:** El API es 100% stateless y puede escalar horizontalmente sin dependencias compartidas como Redis.
- **BaaS ready:** Fácil de integrar por clientes móviles y de terceros enviando el token en el header `Authorization: Bearer <token>`.
- **Integración con Supabase:** Supabase Auth soporta extender los tokens de usuario con `app_metadata` custom a través de triggers y funciones de base de datos.

**Desventajas aceptadas:**
- **Invalidez diferida:** Si a un usuario se le revocan permisos, el cambio no tomará efecto hasta que el token expire o pida un refresh. Para mitigar esto, se configuró un tiempo de expiración (TTL) corto para el JWT (1 hora), apoyándose en Refresh Tokens.
- **Tamaño del token:** El token crece al incluir la lista de permisos, aumentando ligeramente el tamaño del payload HTTP. Se mitiga resumiendo nombres de permisos y manteniendo roles granulares pero limitados.

---

## Consecuencias

### Positivas
- Rendimiento extremadamente alto en endpoints de lectura, al no requerir comprobaciones extras de permisos en DB.
- El API backend es agnóstico del estado de la sesión, facilitando despliegues en contenedores serverless o balanceo de carga simple.
- El middleware de .NET puede inyectar el `tenant_id` directamente desde los claims del JWT al contexto del request, haciendo que fluya naturalmente hacia la BLL y DAL.

### Negativas / Trade-offs aceptados
- Requiere lógica de sincronización: un trigger en PostgreSQL (`on_auth_user_created` o similar) para inyectar el `tenant_id` y `perfil_id` en `auth.users` de Supabase para que se incluyan en el JWT emitido.
- La revocación inmediata de acceso global (ej: despido de empleado) requiere revocar los Refresh Tokens en Supabase Auth y esperar (máx. 1h) a que expire el JWT actual, o implementar una "denylist" en memoria en el API (añadiendo algo de estado), aunque por defecto se acepta la latencia del TTL.

---

## Referencias

- [Supabase Auth - Custom Claims & RBAC](https://supabase.com/docs/guides/auth/custom-claims-and-role-based-access-control-rbac)
- [JWT Best Practices (RFC 8725)](https://datatracker.ietf.org/doc/html/rfc8725)
