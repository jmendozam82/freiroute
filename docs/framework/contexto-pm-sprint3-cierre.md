# Contexto @PM — Cierre Sprint 3 / Apertura Sprint 4

**Fecha:** 2026
**Estado:** Sprint 3 cerrado · Sprint 4 por iniciar
**Para:** Nuevo chat de Sprint 4 en el Proyecto Claude "Freiroute TMS"

---

## Instrucciones para el nuevo chat

Al iniciar el chat de Sprint 4, leer en este orden:
1. AGENTS.md — fuente de verdad del sistema de agentes
2. docs/framework/deuda-tecnica.md — gaps pendientes
3. docs/specs/sprint-04-EP04-order-management.md — cuando se genere
4. Este documento — para contexto de decisiones recientes

---

## Estado del sistema al cerrar Sprint 3

### Infraestructura
- **Tests:** 729/729 en verde (531 BLL + 198 API)
- **Cobertura:** BLL 84.38% · API 80.94%
- **Endpoints:** 98 en Swagger
- **BD cloud:** txulixoybepmrqarosqo — sin drift
- **Puertos locales:** https://localhost:7062 / http://localhost:5102

### Stack confirmado
- ASP.NET Core MVC + Web API (.NET 8)
- Supabase PostgreSQL 15 + Dapper + RLS por empresa_id
- Bootstrap 5.3 + Leaflet.js + CartoDB tiles
- FrApi (freiroute.js v2.0) con Authorization Bearer

---

## Convenciones críticas — NO cambiar sin ADR

| Convención | Valor | ADR |
|---|---|---|
| BusinessException → HTTP | 422 | ADR-009 |
| Create → HTTP | 201 + CreatedAtAction | Sprint 1 fix |
| Logout requiere | [Authorize] | Sprint 1 fix |
| JWT keys | Jwt:Key / Jwt:ExpiryHours | Sprint 1 |
| Módulo permisos empresas | "configuracion" | Sprint 1 |
| Soft delete | DeactivateAsync (nunca DeleteAsync) | ADR-002 |
| Empresa raíz UUID | 00000000-0000-0000-0000-000000000001 | ADR-003 |
| Tarifas | Versionado — nunca UPDATE (CerrarVigenciaAsync) | ADR-015 |
| CSV import | Fail-soft — importar válidas, loguear inválidas | ADR-017 |
| Geocodificación | Background Task.Run — nunca bloquea | ADR-014 |
| Point-in-polygon | Ray casting manual en BLL | ADR-018 |
| TOTP encryption key | Security:TotpEncryptionKey en config | ADR-011 |
| Supabase Storage | Signed URLs on-demand, nunca persistir | ADR-012 |
| Background job | PeriodicTimer, try/catch externo | ADR-013 |

---

## Rutas reales de la API (corregidas en Sprint 3)

Algunas rutas del spec difieren de las rutas reales implementadas.
Usar SIEMPRE las rutas reales al generar prompts:

| Spec decía | Ruta real |
|---|---|
| /api/tarifas | /api/tarifas-base |
| /api/tarifas/simular | /api/tarifas-base/simular-costo |
| /api/zonas | /api/zonas-entrega |
| /api/tipos-mercancia | /api/tipos-mercancia (correcto) |

**Regla:** Antes de emitir un prompt a @FrontendDev o @QA,
verificar las rutas reales en src/Freiroute.API/Controllers/.

---

## Permisos reales del JWT

Los módulos disponibles en RequirePermission son EXACTAMENTE estos
(definidos en ModuloPermiso.cs):

```
ordenes · embarques · carriers · rutas · track_trace
documentos · flota · analytics · facturacion
clientes · usuarios · configuracion
```

⚠️ NO existen módulos "ubicaciones", "tarifas", "zonas"
en los claims del JWT. Las vistas de catálogos usan
"configuracion" o "clientes" como proxy de permisos.

---

## Deuda técnica pendiente al iniciar Sprint 4

Ver deuda-tecnica.md para el detalle completo.

| ID | Gap | Sprint resolución |
|---|---|---|
| G-01 | Permisos Aprobar/Exportar no modelados | Sprint 10 |
| G-03 | Descuento anual automático en planes | Sprint 4 |
| G-05 | Moneda secundaria + idioma del sistema | Sprint 6 |
| G-07 | UbicacionResponseDto campos faltantes | Sprint 4 |
| G-08 | AuditoriaActivityResponseDto sin nombre usuario | Sprint 5 |

---

## Tablas disponibles en BD al iniciar Sprint 4

Sprint 1: empresas · perfiles · permisos · usuarios · invitaciones
          sesiones · auditoria_actividad · configuracion_2fa
          codigos_2fa_temporales

Sprint 2: planes · suscripciones · pagos

Sprint 3: ubicaciones · zonas_entrega · ubicacion_zonas
          tipos_mercancia · unidades_medida · tipos_embalaje
          clientes · contactos_cliente · tarifas_base · recargos_tarifa

---

## HUs diferidas que llegan al Sprint 4

Ninguna — HU-004 y HU-005 se cerraron en Sprint 3.
Sprint 4 arranca limpio con EP-04 Order Management.

---

## Sprint 4 — EP-04 Order Management

### HUs del backlog (sprints 4-5 del plan original)

| HU | Descripción | Pts estimados |
|---|---|---|
| HU-021 | Creación manual de orden de transporte | 13 |
| HU-022 | Importación de órdenes desde CSV/Excel | 8 |
| HU-023 | Recepción de órdenes por API (EDI/REST) | 8 |
| HU-024 | Flujo de estados de la orden | 13 |
| HU-025 | Consolidación de órdenes | 8 |
| HU-026 | División de órdenes (Split) | 5 |
| HU-027 | Órdenes recurrentes y plantillas | 5 |

**Total estimado:** ~60 pts

### Tablas nuevas que requiere Sprint 4

```sql
ordenes              -- tabla principal
lineas_orden         -- items de la orden (mercancía, unidades)
documentos_orden     -- adjuntos por orden
historial_estados    -- máquina de estados de la orden
plantillas_orden     -- para HU-027
```

### ADRs a definir antes del Sprint 4

1. **Máquina de estados de la orden** — estados válidos,
   transiciones permitidas, quién puede hacer cada transición
2. **Numeración automática de órdenes** — usar el prefijo
   configurado en el onboarding (prefijo_orden + consecutivo)
3. **Integración EDI** (HU-023) — alcance del MVP

### Dependencias críticas del Sprint 4

Las órdenes referencian tablas del Sprint 3:
- `clientes.id` → el cliente que origina la orden
- `ubicaciones.id` → origen y destino de la orden
- `tipos_mercancia.id` → tipo de carga
- `unidades_medida.id` → unidad de peso/volumen
- `tipos_embalaje.id` → tipo de embalaje
- `tarifas_base.id` → tarifa que aplica al calcular el costo

Por eso Sprint 3 era prerequisito bloqueante para Sprint 4.

---

## Cómo arrancar el Chat de Sprint 4

Copiar este prompt al nuevo chat del Proyecto:

```
Continuamos el desarrollo de Freiroute TMS.

Sprint 4 — EP-04 Order Management.

Lee antes de empezar:
1. AGENTS.md
2. docs/framework/deuda-tecnica.md
3. docs/framework/contexto-pm-sprint3-cierre.md (este archivo)

Estado: Sprint 3 cerrado ✅
  · 729 tests · 98 endpoints · BD sincronizada
  · Catálogos base disponibles (ubicaciones, clientes, tarifas)
  · Deuda G-03 y G-07 a resolver en este sprint

Generar artefactos del Sprint 4:
  · ADR sobre máquina de estados de órdenes
  · ADR sobre numeración automática
  · Spec HU-021 a HU-027 con tablas SQL y CAs completos

¿Arrancamos con los artefactos?
```

---

*Documento de traspaso Sprint 3 → Sprint 4*
*Generado por @PM al cierre del Sprint 3*
*Freiroute TMS — 2026*
