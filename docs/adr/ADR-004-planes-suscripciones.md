# ADR-004: Gestión de Planes y Suscripciones SaaS

## Estado
Aceptado

## Fecha
2026 — Sprint 2

## Contexto
Freiroute es un SaaS multi-tenant donde cada empresa contrata un plan
de suscripción. El sistema necesita controlar qué funcionalidades están
disponibles por plan, aplicar límites operativos (usuarios, embarques,
storage), gestionar el ciclo de facturación y suspender tenants por
falta de pago — todo sin afectar la arquitectura de datos multi-tenant
existente.

## Decisión
Los planes se gestionan con una tabla `planes` configurable por el
Super Admin. Los límites se verifican en la BLL antes de cada operación
crítica. La facturación en Sprint 2 es manual (registro de pago) — la
integración con pasarela de pago (Stripe) va en Sprint 13 (EP-10).

## Estructura de Planes

| Plan | Usuarios | Embarques/mes | Storage | Módulos |
|---|---|---|---|---|
| STARTER | 5 | 500 | 1 GB | Core (órdenes, embarques, carriers, rutas) |
| PROFESSIONAL | 25 | 5,000 | 10 GB | Core + Analytics + Documentos + Portal cliente |
| ENTERPRISE | Ilimitado | Ilimitado | 100 GB | Todos los módulos |

## Implementación

### Verificación de límites (BLL)
```csharp
// Freiroute.BLL/Interfaces/IPlanLimiteService.cs
public interface IPlanLimiteService
{
    Task VerificarLimiteUsuariosAsync(Guid empresaId);
    Task VerificarLimiteEmbarquesMesAsync(Guid empresaId);
    Task<bool> ModuloDisponibleAsync(string modulo, Guid empresaId);
}
```

Cada servicio de negocio que crea recursos llama al verificador
antes de persistir. Si el límite se excede → `BusinessException`
con mensaje claro al usuario.

### Estados del tenant ampliados
```
TRIAL       → período de prueba (14 días sin pago)
ACTIVE      → suscripción vigente y al día
PAST_DUE    → pago vencido (acceso de solo lectura, 7 días de gracia)
SUSPENDED   → suspendido por falta de pago (acceso bloqueado)
CANCELLED   → cancelado definitivamente
```

### Ciclo de facturación (Sprint 2 — manual)
- El Super Admin registra los pagos manualmente
- El sistema calcula la próxima fecha de vencimiento
- Alertas automáticas: 15, 7 y 1 día antes del vencimiento
- La integración con Stripe se implementa en Sprint 13

## Alternativas Consideradas

1. **Planes hardcodeados en código** — Descartada porque el Super Admin
   necesita poder ajustar límites sin redeploy.

2. **Feature flags por módulo** — Descartada en esta fase por complejidad.
   Los módulos disponibles se derivan del plan y se verifican en middleware.

3. **Stripe desde Sprint 2** — Descartada para no bloquear el desarrollo
   del módulo de administración con una integración externa compleja.
   La facturación manual es suficiente para el MVP.

## Consecuencias

**Positivas:**
- Planes configurables sin redeploy
- Límites verificados en BLL — imposible bypassear desde la UI
- Separación clara entre gestión SaaS (Super Admin) y operación del tenant

**Negativas / Trade-offs:**
- La verificación de límites agrega latencia en operaciones de creación
- Sin pasarela de pago en Sprint 2 — proceso manual de facturación
- Los límites de storage requieren integración con Supabase Storage API

## Módulos Afectados
- EP-02: HU-009 a HU-014 (Sprint 2)
- EP-10: Freight Audit & Payment / SaaS Billing (Sprint 13)
