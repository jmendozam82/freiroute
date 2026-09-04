# ADR-010: Onboarding Wizard Multi-Paso para Nuevos Tenants

## Estado
Aceptado

## Fecha
2026 — Sprint 2

## Contexto
Cuando un nuevo tenant se activa en Freiroute, necesita configurar su
empresa antes de poder operar: datos fiscales, logo, zona horaria,
modos de transporte activos, primer usuario administrador e invitación
del equipo inicial. Sin un wizard guiado, el Admin del tenant queda
frente a un sistema vacío sin saber por dónde empezar.

## Decisión
Implementar un **wizard de 5 pasos** que se activa automáticamente
en el primer login del Admin de un tenant nuevo. El progreso se persiste
en BD por si el usuario interrumpe el proceso. El wizard se puede
retomar desde cualquier paso incompleto.

## Pasos del Wizard

```
Paso 1: Datos de la empresa
  → nombre, RUC/NIT, dirección fiscal, teléfono, industria

Paso 2: Identidad visual
  → logo (PNG/SVG máx 2MB), color primario, color secundario
  → preview en tiempo real del sidebar con los colores elegidos

Paso 3: Configuración operativa
  → país, moneda principal, zona horaria, formato de fecha/hora
  → modos de transporte activos (FTL, LTL, Aéreo, Marítimo, etc.)
  → prefijos de numeración (embarques, órdenes, carta de porte)

Paso 4: Primer administrador
  → confirmar o actualizar datos del admin que activó la cuenta
  → cambiar contraseña temporal si aplica

Paso 5: Invitar equipo
  → hasta 5 invitaciones de email con rol asignado
  → opción "Saltar por ahora"
```

## Implementación

### Persistencia del progreso
```sql
-- Campo en tabla empresas
onboarding_paso_actual   INTEGER NOT NULL DEFAULT 1,
onboarding_completado    BOOLEAN NOT NULL DEFAULT false
```

### Middleware de redirección
```csharp
// Si Admin de tenant hace login y onboarding_completado = false
// → redirigir a /onboarding/paso/{onboarding_paso_actual}
// Rutas excluidas del redirect: /onboarding/*, /auth/*, /api/*
```

### API endpoints del wizard
```
GET  /api/onboarding/estado          → paso actual y % completado
PUT  /api/onboarding/paso/1          → guardar paso 1
PUT  /api/onboarding/paso/2          → guardar paso 2 (con upload logo)
PUT  /api/onboarding/paso/3          → guardar paso 3
PUT  /api/onboarding/paso/4          → guardar paso 4
POST /api/onboarding/paso/5          → enviar invitaciones
POST /api/onboarding/completar       → marcar como completado → Dashboard
```

## Alternativas Consideradas

1. **Formulario único largo** — Descartada porque abruma al usuario
   y tiene alta tasa de abandono en SaaS.

2. **Wizard solo en frontend sin persistencia** — Descartada porque
   si el usuario cierra el browser, pierde el progreso.

3. **Email con checklist de configuración** — Descartada porque
   requiere que el usuario navegue manualmente. El wizard guiado
   garantiza la completitud de la configuración.

## Consecuencias

**Positivas:**
- El tenant queda configurado correctamente antes de operar
- La persistencia permite retomar sin perder datos
- El preview de colores mejora la adopción del white-label

**Negativas / Trade-offs:**
- Requiere 2 campos adicionales en tabla `empresas`
- El middleware de redirección agrega una verificación en cada request
  del Admin (mitigado con cache del estado en el JWT o sesión)

## Módulos Afectados
- HU-012: Onboarding wizard (Sprint 2)
- EP-19: Notificaciones (las invitaciones del paso 5 usan el motor de notificaciones)
