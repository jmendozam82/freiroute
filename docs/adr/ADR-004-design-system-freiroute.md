# ADR-004: Design System Freiroute (colores, tipografía, componentes)

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
Freiroute TMS es un sistema de gestión de transporte utilizado por dispatchers, operadores, conductores y gerentes en entornos operacionales críticos donde la lectura rápida de información visual puede impactar directamente en decisiones logísticas (retardos de entregas, desvíos de ruta, alertas SLA). La interfaz debe transmitir profesionalismo, claridad operativa y consistencia en todas las pantallas del SaaS. No existe una referencia previa de diseño para el proyecto.

## Decisión
El sistema USARÁ el **Design System Freiroute** con las siguientes características fundacionales:

### Paleta de colores semánticos
```css
:root {
  /* Identidad */
  --fr-navy-primary:    #0B2545;   /* Sidebar, navbar, marca */
  --fr-navy-mid:        #1B4F8A;   /* Hover sidebar, gradiente */
  --fr-action-blue:     #1A73E8;   /* Botones CTA, links, acento primario */
  --fr-cyan-accent:     #00D4FF;   /* Logo mark, item activo sidebar */
  --fr-blue-tint:       #E3F0FF;   /* Fondos informativos */

  /* Semántica operacional TMS */
  --fr-success:         #2E7D32;   /* Entregado, OTD positivo */
  --fr-warning:         #F57F17;   /* En tránsito, SLA en riesgo */
  --fr-danger:          #E53935;   /* Crítico, error, POD vencido */
  --fr-info:            #1A73E8;   /* Confirmado, Asignado */
}
```

### Estados operacionales de embarques (mapeo visual estricto)
| Estado | Clase Badge | Color Hex | Icono Tabler |
|---|---|---|---|
| DRAFT | `badge-fr-neutral` | `#64748B` | `ti-circle-dot` |
| CONFIRMED | `badge-fr-info` | `#1A73E8` | `ti-circle-check` |
| ASSIGNED | `badge-fr-info` | `#0891B2` | `ti-truck` |
| IN_TRANSIT | `badge-fr-warning` | `#F57F17` | `ti-truck-loading` |
| DELIVERED | `badge-fr-success` | `#2E7D32` | `ti-package-check` |
| FAILED_DELIVERY | `badge-fr-danger` | `#E53935` | `ti-circle-x` |
| ON_HOLD | `badge-fr-onhold` | `#C2410C` | `ti-clock-hour-4` |
| CANCELLED | `badge-fr-neutral` | `#374151` | `ti-ban` |

### Tipografía
| Rol | Fuente | Uso |
|---|---|---|
| Inter 400/500/600/700 | UI Principal | Sidebar, tablas, formularios, dashboards |
| DM Sans 400/500/700 | Display / Marketing | Landing page, portal cliente, onboarding |
| JetBrains Mono 400/500 | Datos / IDs | Números de embarque, UUIDs, códigos |

### Componentes estándar
- Cards con `border-radius: 10px`, `border: 1px solid #E2E8F0`
- Badges operacionales (`badge-fr-*`)
- KPI cards con `kpi-label`, `kpi-value`, `kpi-delta`
- Sidebar expandido 240px con items activos resaltados en cyan
- Tablas sin borde externo, filas separadas por `border-bottom`
- Paginado: 20 registros/página (RNF-01.4)
- Toast notifications con bootstrap 5.3

## Alternativas Consideradas
1. **Bootstrap + temas personalizados** — Descartado porque no ofrece diferenciación competitiva ni identidad propia frente a otros TMS genéricos.
2. **Tailwind CSS** — Demasiada verbosidad inline en vistas Razor MVC que dificulta mantenimiento consistente entre desarrolladores y agentes IA.
3. **Material Design (MUI)** — Incompatible con stack Bootstrap 5.3 ya establecido y con estética enterprise navy/corporativa del dominio TMS.

## Consecuencias
**Positivas:**
- Consistencia visual absoluta entre todos los módulos del MVP
- Identidad de marca propia frente a competidores
- Reducción de tiempo de desarrollo frontend al reusar patrones existentes
- Escalable: cada nuevo módulo aplica automáticamente el sistema

**Negativas / Trade-offs:**
- Requiere mantener archivo `freiroute.css` como fuente de verdad única
- Cambios estéticos globales implican afectar TODAS las vistas existentes
- Curva de aprendizaje inicial para nuevos developers en las clases semánticas personalizadas

## Módulos Afectados
Todas las vistas Razor de `Freiroute.Aplicacion`. Este ADR define la capa de presentación completa y afecta a @FrontendDev exclusivamente durante toda la vida del proyecto.

---
