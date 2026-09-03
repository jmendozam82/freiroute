# Freiroute Design System v1.0
**Sistema de Diseño Visual — Freiroute TMS SaaS Multi-Tenant**
Referencia: Oracle TMS · SAP TM · Trimble Modus · MercuryGate · BluJay Solutions
Fecha: 2026 | Versión: 1.0.0

> **Lectura obligatoria para @FrontendDev antes de tocar cualquier vista.**
> Este documento es la fuente de verdad para toda decisión visual del sistema.
> Ningún color, fuente, espaciado o componente debe inventarse fuera de este sistema.

---

## Índice

1. [Principios de Diseño](#1-principios-de-diseño)
2. [Paleta de Color](#2-paleta-de-color)
3. [Tipografía](#3-tipografía)
4. [Espaciado y Layout](#4-espaciado-y-layout)
5. [Sidebar](#5-sidebar)
6. [Topbar](#6-topbar)
7. [Cards y Paneles](#7-cards-y-paneles)
8. [KPI Cards — Dashboard](#8-kpi-cards--dashboard)
9. [Tablas](#9-tablas)
10. [Badges de Estado TMS](#10-badges-de-estado-tms)
11. [Botones](#11-botones)
12. [Formularios e Inputs](#12-formularios-e-inputs)
13. [Paginación](#13-paginación)
14. [Notificaciones y Toasts](#14-notificaciones-y-toasts)
15. [Iconografía](#15-iconografía)
16. [Mapas y Track & Trace](#16-mapas-y-track--trace)
17. [Responsive y Breakpoints](#17-responsive-y-breakpoints)
18. [Variables CSS — Referencia Completa](#18-variables-css--referencia-completa)
19. [Checklist de Conformidad UI](#19-checklist-de-conformidad-ui)

---

## 1. Principios de Diseño

### 1.1 Filosofía central

Freiroute es una herramienta de trabajo, no una vitrina. Los usuarios — dispatchers, operadores, gerentes de flota — viven 8 o más horas diarias frente a esta interfaz tomando decisiones de transporte en tiempo real. Cada decisión de diseño debe responder a una sola pregunta:

> **¿Esto ayuda al usuario a tomar decisiones más rápido y con menos errores?**

### 1.2 Principios operativos

| Principio | Descripción | Implicación práctica |
|---|---|---|
| **Densidad informativa** | Mostrar la mayor cantidad de datos relevantes sin saturar | Tablas compactas, KPIs siempre visibles, sin carruseles ni animaciones innecesarias |
| **Semántica de color** | Cada color tiene un significado único y consistente | Verde = bien, ámbar = atención, rojo = crítico — siempre, en todas las pantallas |
| **Jerarquía clara** | El ojo debe ir primero a lo más importante | Títulos grandes, números KPI prominentes, acciones primarias en azul |
| **Feedback inmediato** | Toda acción del usuario recibe respuesta visual en < 300ms | Toasts, spinners, estados de carga, cambio de color al hover |
| **Accesibilidad AA** | Contraste mínimo 4.5:1 en texto body | Nunca usar texto gris claro sobre fondo blanco para información crítica |
| **Consistencia** | Mismo componente, mismo comportamiento, en todos los módulos | Un badge "En tránsito" siempre es ámbar, en órdenes, embarques y tracking |

### 1.3 Lo que NO hacer

- ❌ Inventar colores fuera de la paleta definida
- ❌ Usar `alert()` nativo del navegador — siempre `FrToast`
- ❌ Hardcodear colores en atributos `style=""` — solo variables `var(--fr-*)`
- ❌ Eliminar el sidebar en pantallas de escritorio
- ❌ Usar más de 2 pesos de fuente en una misma pantalla
- ❌ Mostrar spinners de carga de página completa para operaciones parciales
- ❌ Usar Bootstrap directamente sin pasar por las clases `fr-*` equivalentes

---

## 2. Paleta de Color

### 2.1 Colores de identidad Freiroute

Estos colores definen la marca. Se usan en sidebar, logo, topbar y elementos de navegación principal.

| Token CSS | Hex | RGB | Uso |
|---|---|---|---|
| `--fr-navy-primary` | `#0B2545` | 11, 37, 69 | Sidebar fondo, logo fondo oscuro |
| `--fr-navy-mid` | `#1B4F8A` | 27, 79, 138 | Hover items sidebar, gradientes |
| `--fr-navy-light` | `#2C6BAD` | 44, 107, 173 | Elementos secundarios sobre navy |
| `--fr-action-blue` | `#1A73E8` | 26, 115, 232 | Botones CTA, links, acento primario |
| `--fr-action-hover` | `#1557B0` | 21, 87, 176 | Hover sobre botón primario |
| `--fr-cyan-accent` | `#00D4FF` | 0, 212, 255 | Logo mark, ítem activo sidebar, highlights especiales |
| `--fr-blue-tint` | `#E3F0FF` | 227, 240, 255 | Fondo de tarjetas informativas, badges info |

**Regla de uso del cyan `#00D4FF`:** Solo se usa sobre fondo navy oscuro (`#0B2545`) o como color de texto/borde de ítem activo. Nunca sobre fondo blanco o gris claro — el contraste es insuficiente.

### 2.2 Colores semánticos operacionales

Estos colores comunican estado. Su significado es fijo e inamovible en todo el sistema.

| Token CSS | Hex | Significado TMS | Uso |
|---|---|---|---|
| `--fr-success` | `#2E7D32` | Entregado · OTD cumplido · Documento OK · Carrier activo | Badges, KPIs positivos, íconos de confirmación |
| `--fr-success-light` | `#E6F4EA` | — | Fondo de badges y cards success |
| `--fr-success-border` | `#A5D6A7` | — | Borde de elementos success |
| `--fr-warning` | `#F57F17` | En tránsito · SLA en riesgo · Documento por vencer · Alerta | Badges, KPIs de atención, alertas |
| `--fr-warning-light` | `#FFF8E1` | — | Fondo de badges y cards warning |
| `--fr-warning-border` | `#FFE082` | — | Borde de elementos warning |
| `--fr-danger` | `#E53935` | Retrasado · Crítico · Error · Documento vencido · Bloqueado | Badges, KPIs críticos, mensajes de error |
| `--fr-danger-light` | `#FFEBEE` | — | Fondo de badges y cards danger |
| `--fr-danger-border` | `#EF9A9A` | — | Borde de elementos danger |
| `--fr-info` | `#1A73E8` | Confirmado · Asignado · Programado · Informativo | Badges info, KPIs de actividad |
| `--fr-info-light` | `#E3F0FF` | — | Fondo de badges y cards info |

### 2.3 Colores neutros

| Token CSS | Hex | Uso |
|---|---|---|
| `--fr-surface-bg` | `#F8FAFC` | Fondo de página (body background) |
| `--fr-surface-card` | `#FFFFFF` | Fondo de cards, modales, formularios |
| `--fr-surface-hover` | `#F1F5F9` | Fondo hover de filas y elementos interactivos |
| `--fr-surface-disabled` | `#F8FAFC` | Fondo de elementos deshabilitados |
| `--fr-border` | `#E2E8F0` | Bordes estándar de cards y tablas |
| `--fr-border-strong` | `#CBD5E1` | Bordes de inputs en reposo |
| `--fr-border-focus` | `#1A73E8` | Borde de input con foco |
| `--fr-text-primary` | `#1E293B` | Texto principal, headings |
| `--fr-text-secondary` | `#475569` | Texto secundario, descripciones |
| `--fr-text-muted` | `#64748B` | Labels, hints, texto de apoyo |
| `--fr-text-disabled` | `#94A3B8` | Texto de elementos deshabilitados |
| `--fr-text-inverse` | `#FFFFFF` | Texto sobre fondos oscuros (navy, botones) |

### 2.4 Sombras

| Token CSS | Valor | Uso |
|---|---|---|
| `--fr-shadow-xs` | `0 1px 2px rgba(0,0,0,.05)` | Inputs, elementos pequeños |
| `--fr-shadow-sm` | `0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.06)` | Cards, topbar |
| `--fr-shadow-md` | `0 4px 12px rgba(0,0,0,.10), 0 2px 4px rgba(0,0,0,.06)` | Dropdowns, modales, toasts |
| `--fr-shadow-lg` | `0 10px 25px rgba(0,0,0,.12), 0 4px 6px rgba(0,0,0,.07)` | Modales grandes, popovers |

---

## 3. Tipografía

### 3.1 Familias tipográficas

| Rol | Fuente | CDN Google Fonts | Uso |
|---|---|---|---|
| **UI Principal** | Inter (Variable) | `family=Inter:wght@400;500;600;700` | Todo el sistema: sidebar, tablas, formularios, dashboards, modales |
| **Display / Marketing** | DM Sans | `family=DM+Sans:wght@400;500;700` | Portal del cliente, landing page, onboarding wizard, emails |
| **Datos / Códigos** | JetBrains Mono | `family=JetBrains+Mono:wght@400;500` | Números de embarque, IDs UUID, coordenadas, snippets de código |

**Import CSS obligatorio en `_Layout.cshtml`:**
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=DM+Sans:wght@400;500;700&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
```

### 3.2 Escala tipográfica

Razón de escala: **1.2 (Minor Third)** — produce una jerarquía clara sin saltos bruscos.

| Nombre | Clase / Token | Tamaño | Peso | Color | Line Height | Uso |
|---|---|---|---|---|---|---|
| Page Title | `.fr-page-title` | 22px | 700 | `--fr-text-primary` | 1.25 | Título de página (H1) |
| Module Title | `.fr-module-title` | 18px | 700 | `--fr-text-primary` | 1.3 | Título de módulo o sección principal |
| Card Title | `.fr-card-title` | 14px | 600 | `--fr-text-primary` | 1.4 | Título de card o panel |
| Body | `.fr-body` | 13px | 400 | `--fr-text-primary` | 1.6 | Texto de párrafo, descripciones |
| Body Strong | `.fr-body-strong` | 13px | 600 | `--fr-text-primary` | 1.6 | Énfasis en texto de párrafo |
| Table Header | `.fr-table-header` | 10.5px | 600 | `--fr-text-muted` | — | Encabezados de columna (UPPERCASE) |
| Table Cell | `.fr-table-cell` | 12.5px | 400 | `--fr-text-primary` | — | Contenido de celdas |
| Label | `.fr-label` | 12px | 600 | `--fr-text-secondary` | — | Labels de formulario |
| Hint / Helper | `.fr-hint` | 11px | 400 | `--fr-text-muted` | 1.5 | Textos de ayuda debajo de inputs |
| Badge | `.fr-badge` | 10.5px | 600 | variable | — | Badges de estado |
| Code / ID | `.fr-id-code` | 12px | 500 | `--fr-action-blue` | — | Números de embarque, IDs, códigos |
| KPI Value | `.fr-kpi-value` | 28px | 700 | variable | 1 | Valor principal de KPI card |
| KPI Label | `.fr-kpi-label` | 11px | 600 | `--fr-text-muted` | — | Etiqueta de KPI (UPPERCASE) |

### 3.3 Reglas tipográficas

- Nunca usar más de **2 pesos** de Inter en una misma pantalla (400 + 600, o 500 + 700)
- Los **table headers** siempre en `UPPERCASE` con `letter-spacing: 0.05em`
- Los **sidebar group labels** siempre en `UPPERCASE` con `letter-spacing: 0.12em`
- Los **números de embarque y códigos** siempre en JetBrains Mono
- Los **KPI values** pueden usar colores semánticos según el dato que representan

---

## 4. Espaciado y Layout

### 4.1 Escala de espaciado

Basada en múltiplos de 4px (grid de 4pt).

| Token | Valor | Uso típico |
|---|---|---|
| `--fr-space-1` | 4px | Gap mínimo entre elementos inline |
| `--fr-space-2` | 8px | Padding interno de badges, gap entre íconos |
| `--fr-space-3` | 12px | Padding de botones pequeños, gap de formularios compactos |
| `--fr-space-4` | 16px | Padding estándar de cards, gap de grillas |
| `--fr-space-5` | 20px | Padding de card body |
| `--fr-space-6` | 24px | Padding del área de contenido principal |
| `--fr-space-8` | 32px | Separación entre secciones mayores |
| `--fr-space-12` | 48px | Empty states, espaciado de onboarding |

### 4.2 Layout principal

```
┌─────────────────────────────────────────────────────┐
│  SIDEBAR (240px)  │  TOPBAR (56px alto, full width)  │
│  fondo #0B2545    ├─────────────────────────────────│
│                   │                                   │
│  Logo Freiroute   │  CONTENIDO PRINCIPAL              │
│  ─────────────    │  padding: 24px                    │
│  [nav items]      │  background: #F8FAFC              │
│                   │                                   │
│                   │  [Page Header]                    │
│                   │  [KPI Grid]                       │
│                   │  [Tabla / Cards]                  │
│                   │                                   │
└───────────────────┴───────────────────────────────────┘
```

| Elemento | Dimensión | Notas |
|---|---|---|
| Sidebar expandido | 240px ancho | Posición `fixed`, full height |
| Sidebar colapsado | 64px ancho | Solo íconos, tooltip en hover |
| Topbar | 56px alto | Posición `sticky top: 0`, `z-index: 100` |
| Contenido | `calc(100vw - 240px)` | Padding 24px en todos los lados |
| Card máxima en formularios | 760px | Para formularios de creación/edición |
| Grid de KPIs | `repeat(auto-fit, minmax(200px, 1fr))` | Auto-adaptable |

### 4.3 Border radius

| Token | Valor | Uso |
|---|---|---|
| `--fr-radius-xs` | 4px | Badges, chips pequeños |
| `--fr-radius-sm` | 6px | Botones, inputs, tooltips |
| `--fr-radius-md` | 10px | Cards, paneles, modales |
| `--fr-radius-lg` | 14px | Modales grandes, drawers |
| `--fr-radius-full` | 9999px | Badges de estado, avatares |

---

## 5. Sidebar

### 5.1 Estructura visual

```
┌────────────────────────────┐
│ [■] Freiroute              │  ← Logo: marca cyan sobre navy
│     TMS · Empresa XYZ      │  ← Nombre tenant en gris claro
├────────────────────────────│
│ PRINCIPAL          ← group │
│  ⊞ Dashboard       ← item  │
│  📋 Órdenes                │
│ ▶ Embarques       ← activo │  ← fondo cyan tint + borde cyan derecha
├────────────────────────────│
│ OPERACIÓN          ← group │
│  🗺 Rutas                  │
│  📍 Track & Trace          │
│  🏢 Carriers               │
├────────────────────────────│
│ INTELIGENCIA       ← group │
│  📊 Analytics              │
├────────────────────────────│
│ (footer: usuario + rol)    │
└────────────────────────────┘
```

### 5.2 Especificaciones

| Estado | Background | Color texto | Border right | Font weight |
|---|---|---|---|---|
| Normal | transparent | `rgba(255,255,255,.6)` | none | 500 |
| Hover | `rgba(255,255,255,.06)` | `rgba(255,255,255,.9)` | none | 500 |
| Activo | `rgba(0,212,255,.12)` | `#00D4FF` | `2px solid #00D4FF` | 600 |
| Deshabilitado | transparent | `rgba(255,255,255,.25)` | none | 400 |

### 5.3 Logo mark

- Tamaño: 32×32px
- Background: `#00D4FF` (cyan)
- Border radius: 7px
- Texto: "FR" en Inter 800, color `#0B2545`
- Efecto: ninguno (sin sombra, sin gradiente)

### 5.4 Group labels

- Font: Inter 600, 9px
- Color: `rgba(255,255,255,.3)`
- Transform: UPPERCASE
- Letter spacing: 0.12em
- Padding: `16px 16px 4px`
- Sin separador visual (solo espacio)

### 5.5 Transición de colapso

```css
.fr-sidebar { transition: width 0.25s ease; }
.fr-sidebar.collapsed { width: 64px; }
.fr-sidebar.collapsed .fr-logo-text,
.fr-sidebar.collapsed .fr-logo-tag,
.fr-sidebar.collapsed .fr-sidebar-group,
.fr-sidebar.collapsed .fr-sidebar-item span { display: none; }
.fr-sidebar.collapsed .fr-sidebar-item { justify-content: center; padding: 9px 0; }
```

---

## 6. Topbar

### 6.1 Especificaciones

| Propiedad | Valor |
|---|---|
| Alto | 56px |
| Background | `#FFFFFF` |
| Border bottom | `1px solid #E2E8F0` |
| Box shadow | `0 1px 3px rgba(0,0,0,.08)` |
| Position | `sticky`, `top: 0`, `z-index: 100` |
| Padding | `0 24px` |

### 6.2 Contenido (de izquierda a derecha)

1. **Botón toggle sidebar** — ícono `ti-menu-2`, clase `fr-btn-ghost fr-btn-icon`
2. **Título de página** — Inter 600, 15px, `#1E293B` — tomado de `ViewData["Title"]`
3. **Spacer** — `ms-auto` (empuja lo siguiente a la derecha)
4. **Botón notificaciones** — ícono `ti-bell` con badge rojo si hay alertas
5. **Dropdown de usuario** — nombre + rol, opciones: Mi perfil / Cerrar sesión

---

## 7. Cards y Paneles

### 7.1 Card estándar

```
┌─────────────────────────────────────┐  ← border: 1px solid #E2E8F0
│ Card Header                          │  ← padding: 16px 20px
│ Título de la card          [Acción]  │  ← border-bottom: 1px solid #E2E8F0
├──────────────────────────────────────│
│                                      │  ← padding: 20px
│  Contenido de la card                │
│                                      │
└──────────────────────────────────────┘
```

| Propiedad | Valor |
|---|---|
| Background | `#FFFFFF` |
| Border | `1px solid #E2E8F0` |
| Border radius | `10px` |
| Box shadow | `0 1px 3px rgba(0,0,0,.08)` |
| Header padding | `16px 20px` |
| Body padding | `20px` |
| Header border bottom | `1px solid #E2E8F0` |
| Header background | `#FFFFFF` (no diferente del body) |

### 7.2 Card de alerta / destacada

Para paneles de información crítica (ej: SLA vencidos, errores):

```css
.fr-card-warning { border-left: 3px solid var(--fr-warning); }
.fr-card-danger  { border-left: 3px solid var(--fr-danger);  }
.fr-card-success { border-left: 3px solid var(--fr-success); }
.fr-card-info    { border-left: 3px solid var(--fr-action-blue); }
```

---

## 8. KPI Cards — Dashboard

### 8.1 Estructura

```
┌────────────────────────────┐
│ EMBARQUES HOY     ← label  │  Inter 600 · 11px · UPPERCASE · muted
│                            │
│ 148               ← value  │  Inter 700 · 28px · color semántico
│                            │
│ ↑ 12% vs. ayer   ← delta  │  Inter 500 · 11px · verde/rojo
└────────────────────────────┘
```

### 8.2 Colores de valores KPI

| Tipo de dato | Color del value | Token |
|---|---|---|
| Embarques activos, volumen | Azul | `--fr-action-blue` |
| OTD %, entregas exitosas | Verde | `--fr-success` |
| En tránsito, en proceso | Ámbar | `--fr-warning` |
| Retrasados, errores, críticos | Rojo | `--fr-danger` |
| Costos, tiempos neutros | Primario | `--fr-text-primary` |

### 8.3 Deltas

- **Positivo (mejora):** `↑` + porcentaje en verde `--fr-success`
- **Negativo (deterioro):** `↓` + porcentaje en rojo `--fr-danger`
- **Neutral:** solo el valor de comparación en gris `--fr-text-muted`

### 8.4 Grid

```css
.fr-kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px;
  margin-bottom: 24px;
}
```

---

## 9. Tablas

### 9.1 Anatomía

```
┌──────────────────────────────────────────────────────────────┐
│  N° EMBARQUE   ORIGEN    DESTINO    CARRIER    ESTADO    ···  │  ← thead: uppercase, 10.5px, muted
├──────────────────────────────────────────────────────────────│
│  FR-2026-0847  Managua   León       Trans S.A  [badge]   ···  │  ← tbody: 12.5px, hover highlight
│  FR-2026-0846  Masaya    Granada    CargoMx    [badge]   ···  │
│  FR-2026-0845  León      Rivas      FleetNic   [badge]   ···  │
├──────────────────────────────────────────────────────────────│
│  Mostrando 1–20 de 347 registros          [← Anterior] [Siguiente →]│  ← pagination footer
└──────────────────────────────────────────────────────────────┘
```

### 9.2 Especificaciones

| Elemento | Valor |
|---|---|
| Wrapper border | `1px solid #E2E8F0`, border-radius `10px`, overflow `hidden` |
| thead background | `#F8FAFC` |
| thead padding celda | `10px 14px` |
| thead font | Inter 600, 10.5px, UPPERCASE, `#64748B`, letter-spacing `.05em` |
| thead border bottom | `1px solid #E2E8F0` |
| tbody padding celda | `11px 14px` |
| tbody font | Inter 400, 12.5px, `#1E293B` |
| tbody border row | `border-bottom: 1px solid #E2E8F0` |
| tbody hover | background `#F1F5F9` |
| última fila | sin border-bottom |
| columna de acciones | width `120px`, alineación center |

### 9.3 Columna de números de embarque / IDs

Siempre con clase `fr-id-code`:
```css
.fr-id-code {
  font-family: 'JetBrains Mono', monospace;
  font-size: 12px;
  font-weight: 500;
  color: var(--fr-action-blue);
}
```

### 9.4 Columna de acciones

Máximo 3 íconos por fila. Siempre en este orden: Ver → Editar → Desactivar.

```html
<div class="d-flex gap-1">
  <a class="fr-btn fr-btn-ghost fr-btn-icon fr-btn-sm" title="Ver">
    <i class="ti ti-eye"></i>
  </a>
  <a class="fr-btn fr-btn-ghost fr-btn-icon fr-btn-sm" title="Editar">
    <i class="ti ti-pencil"></i>
  </a>
  <button class="fr-btn fr-btn-ghost fr-btn-icon fr-btn-sm" title="Desactivar">
    <i class="ti ti-trash" style="color:var(--fr-danger)"></i>
  </button>
</div>
```

### 9.5 Estado vacío de tabla

```html
<div class="fr-empty">
  <div class="fr-empty-icon"><i class="ti ti-inbox"></i></div>
  <div class="fr-empty-title">No hay [registros] disponibles</div>
  <div class="fr-empty-text">Crea el primero usando el botón "[Acción]"</div>
</div>
```

---

## 10. Badges de Estado TMS

### 10.1 Mapa completo de estados

Los badges son la comunicación visual más crítica del sistema. Su color es **inamovible** — no depende del contexto, siempre es el mismo para cada estado.

| Estado | Label ES | Clase CSS | Color texto | Color fondo |
|---|---|---|---|---|
| `DRAFT` | Borrador | `fr-badge-neutral` | `#64748B` | `#F1F5F9` |
| `CONFIRMED` | Confirmado | `fr-badge-info` | `#1A73E8` | `#E3F0FF` |
| `ASSIGNED` | Asignado | `fr-badge-info` | `#1A73E8` | `#E3F0FF` |
| `PICKUP_SCHEDULED` | Pickup programado | `fr-badge-info` | `#1A73E8` | `#E3F0FF` |
| `IN_TRANSIT` | En tránsito | `fr-badge-warning` | `#F57F17` | `#FFF8E1` |
| `ON_HOLD` | En espera | `fr-badge-warning` | `#C2410C` | `#FFF3E0` |
| `DELIVERED` | Entregado | `fr-badge-success` | `#2E7D32` | `#E6F4EA` |
| `INVOICED` | Facturado | `fr-badge-success` | `#2E7D32` | `#E6F4EA` |
| `CLOSED` | Cerrado | `fr-badge-neutral` | `#475569` | `#F1F5F9` |
| `CANCELLED` | Cancelado | `fr-badge-danger` | `#E53935` | `#FFEBEE` |
| `FAILED_DELIVERY` | Entrega fallida | `fr-badge-danger` | `#E53935` | `#FFEBEE` |

### 10.2 Badges de documentos

| Estado | Label | Clase |
|---|---|---|
| `VIGENTE` | Vigente | `fr-badge-success` |
| `POR_VENCER` | Por vencer | `fr-badge-warning` |
| `VENCIDO` | Vencido | `fr-badge-danger` |

### 10.3 Badges de carrier / score

| Score | Label | Clase |
|---|---|---|
| 80–100 | Oro | `fr-badge-success` |
| 60–79 | Plata | `fr-badge-info` |
| 40–59 | Bronce | `fr-badge-warning` |
| 0–39 | En observación | `fr-badge-danger` |

### 10.4 CSS de badges

```css
.fr-badge {
  display: inline-flex;
  align-items: center;
  padding: 3px 10px;
  border-radius: 9999px;
  font-size: 10.5px;
  font-weight: 600;
  letter-spacing: .02em;
  white-space: nowrap;
}
.fr-badge-success { background: #E6F4EA; color: #2E7D32; }
.fr-badge-warning { background: #FFF8E1; color: #F57F17; }
.fr-badge-danger  { background: #FFEBEE; color: #E53935; }
.fr-badge-info    { background: #E3F0FF; color: #1A73E8; }
.fr-badge-neutral { background: #F1F5F9; color: #64748B;
                    border: 1px solid #E2E8F0; }
```

---

## 11. Botones

### 11.1 Variantes

| Variante | Clase | Background | Texto | Borde | Uso |
|---|---|---|---|---|---|
| Primary | `fr-btn-primary` | `#1A73E8` | `#fff` | ninguno | Acción principal de la página |
| Secondary | `fr-btn-secondary` | transparent | `#1A73E8` | `1.5px solid #1A73E8` | Acción secundaria |
| Success | `fr-btn-success` | `#2E7D32` | `#fff` | ninguno | Confirmar, aprobar, entregar |
| Danger | `fr-btn-danger` | `#E53935` | `#fff` | ninguno | Cancelar, rechazar, eliminar modal |
| Ghost | `fr-btn-ghost` | `#F8FAFC` | `#475569` | `1px solid #E2E8F0` | Acciones terciarias, cancelar |
| Icon | `fr-btn-icon` | igual al base | — | — | Botón solo ícono, padding cuadrado |

### 11.2 Tamaños

| Tamaño | Clase | Padding | Font size |
|---|---|---|---|
| Normal | `fr-btn` | `8px 16px` | 12.5px |
| Small | `fr-btn fr-btn-sm` | `5px 10px` | 11.5px |
| Icon normal | `fr-btn fr-btn-icon` | `7px` | — |
| Icon small | `fr-btn fr-btn-icon fr-btn-sm` | `5px` | — |

### 11.3 Estados

- **Hover:** oscurecer background 8% o añadir `background: var(--fr-blue-tint)` en secondary/ghost
- **Disabled:** `opacity: 0.45`, `cursor: not-allowed`
- **Loading:** reemplazar texto con spinner + "Guardando..." y `disabled: true`

### 11.4 Regla de página

Cada página tiene **máximo 1 botón Primary**. El resto son Secondary o Ghost.

---

## 12. Formularios e Inputs

### 12.1 Input estándar

```
[Label obligatorio *]
┌─────────────────────────────┐
│ Valor o placeholder          │  ← borde #CBD5E1, 1px
└─────────────────────────────┘
  Texto de ayuda (si aplica)     ← 11px, muted

                    ↓ focus

[Label obligatorio *]
┌─────────────────────────────┐
│ Valor o placeholder          │  ← borde #1A73E8, 1px + ring azul
└─────────────────────────────┘

                    ↓ error

[Label obligatorio *]
┌─────────────────────────────┐
│ Valor o placeholder          │  ← borde #E53935, 1px
└─────────────────────────────┘
  ⚠ El nombre es obligatorio     ← 11px, rojo #E53935
```

### 12.2 Especificaciones de input

| Estado | Border | Box shadow | Background |
|---|---|---|---|
| Normal | `1px solid #CBD5E1` | ninguna | `#FFFFFF` |
| Focus | `1px solid #1A73E8` | `0 0 0 3px rgba(26,115,232,.15)` | `#FFFFFF` |
| Error | `1px solid #E53935` | `0 0 0 3px rgba(229,57,53,.15)` | `#FFFFFF` |
| Disabled | `1px solid #E2E8F0` | ninguna | `#F8FAFC` |

Padding interno: `8px 12px` | Border radius: `6px` | Font: Inter 400, 13px

### 12.3 Label

- Font: Inter 600, 12px, `#475569`
- Indicador de obligatorio: ` *` en `#E53935` después del label
- Margin bottom: 5px

### 12.4 Validación

- **Cliente:** jQuery Validate con `data-val-*` attributes — mensajes en español
- **Servidor:** FluentValidation — mismos mensajes, retornados en `ApiResponse.Errors`
- Nunca confiar solo en validación cliente
- El error se muestra debajo del input con clase `fr-form-error` (11px, rojo)

### 12.5 Estructura de formulario de creación

```
[fr-card style="max-width: 760px"]
  [fr-card-header]
    Título del formulario
  [fr-card-body]
    [row g-3]
      [col-md-8] Campo principal
      [col-md-4] Campo secundario
      [col-12]   Campo de texto largo / textarea
    [div mt-4 pt-3 border-top]
      [fr-btn-primary] Crear / Guardar cambios
      [fr-btn-ghost]   Cancelar
```

---

## 13. Paginación

### 13.1 Especificaciones

- **Registros por página:** 20 (fijo, según RNF-01.4 del proyecto)
- **Posición:** footer de la tabla wrapper
- **Border top:** `1px solid #E2E8F0`
- **Padding:** `12px 16px`

### 13.2 Información de paginación

```
Mostrando 21–40 de 347 registros          [← Anterior]  [Siguiente →]
```

- Texto izquierda: Inter 400, 12px, `#64748B`
- Botones: `fr-page-btn` — borde suave, hover azul
- Botón deshabilitado: `opacity: 0.4`, `cursor: not-allowed`

---

## 14. Notificaciones y Toasts

### 14.1 Sistema de toasts

Los toasts son la única forma de comunicar resultados de operaciones al usuario. **Nunca usar `alert()` nativo.**

```
                               ┌──────────────────────────────────────┐
                               │ [✓] Éxito                         [×]│  ← borde izq. verde
                               │     Embarque creado exitosamente      │
                               └──────────────────────────────────────┘
```

| Tipo | Borde izquierdo | Ícono | Cuándo usar |
|---|---|---|---|
| Success | `#2E7D32` | `ti-circle-check` | Operación completada |
| Error | `#E53935` | `ti-circle-x` | Error de servidor, validación fallida |
| Warning | `#F57F17` | `ti-alert-triangle` | Advertencia, acción con riesgo |
| Info | `#1A73E8` | `ti-info-circle` | Información general, estado |

### 14.2 Comportamiento

- Posición: `fixed`, top-right, `z-index: 9999`
- Duración: 4500ms auto-dismiss
- Máximo simultáneos: 3 (el más antiguo se elimina al aparecer el 4to)
- Animación entrada: `translateX(20px)` → `translateX(0)`, 200ms ease
- El usuario puede cerrar manualmente con el botón `[×]`

### 14.3 Alertas operacionales (panel de alertas)

Para alertas persistentes del sistema (SLA en riesgo, documentos por vencer), usar el **Centro de Alertas** en el topbar — no toasts. Los toasts son para resultados de acciones del usuario, no para alertas del sistema.

---

## 15. Iconografía

### 15.1 Biblioteca: Tabler Icons

CDN:
```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@3.0.0/dist/tabler-icons.min.css" />
```

Uso: `<i class="ti ti-[nombre]"></i>`

### 15.2 Íconos estándar por módulo TMS

| Módulo / Función | Ícono Tabler | Clase |
|---|---|---|
| Dashboard | Layout Dashboard | `ti-layout-dashboard` |
| Órdenes | Clipboard List | `ti-clipboard-list` |
| Embarques | Truck | `ti-truck` |
| Carriers | Building Warehouse | `ti-building-warehouse` |
| Conductores | User Star | `ti-user-star` |
| Vehículos | Car | `ti-car` |
| Rutas | Map Route | `ti-map-route` |
| Track & Trace | Map Pin | `ti-map-pin` |
| Analytics | Chart Bar | `ti-chart-bar` |
| Documentos | File Text | `ti-file-text` |
| Facturación | Receipt | `ti-receipt` |
| Usuarios | Users | `ti-users` |
| Configuración | Settings | `ti-settings` |
| Empresas (SaaS) | Building | `ti-building` |
| Notificaciones | Bell | `ti-bell` |
| Crear / Nuevo | Plus | `ti-plus` |
| Editar | Pencil | `ti-pencil` |
| Ver detalle | Eye | `ti-eye` |
| Desactivar | Trash | `ti-trash` |
| Exportar | Table Export | `ti-table-export` |
| Buscar | Search | `ti-search` |
| Filtrar | Filter | `ti-filter` |
| Guardar | Device Floppy | `ti-device-floppy` |
| Cerrar sesión | Logout | `ti-logout` |
| Menú toggle | Menu 2 | `ti-menu-2` |
| Alerta | Alert Triangle | `ti-alert-triangle` |
| Éxito | Circle Check | `ti-circle-check` |
| Error | Circle X | `ti-circle-x` |
| GPS / Posición | Current Location | `ti-current-location` |
| POD / Firma | Signature | `ti-signature` |
| Carga | Package | `ti-package` |

### 15.3 Tamaños de íconos

| Contexto | Tamaño CSS | Notas |
|---|---|---|
| Sidebar items | `font-size: 16px` | Con texto al lado |
| Botones icon-only | `font-size: 16px` | Padding cuadrado |
| Botones con texto | `font-size: 14px` | Gap 6px con el texto |
| Toasts | `font-size: 18px` | Color semántico |
| Empty state | `font-size: 40px` | Opacity 0.4 |
| KPI cards | `font-size: 20px` | Color semántico |

---

## 16. Mapas y Track & Trace

### 16.1 Proveedor de mapas

**OpenStreetMap con Leaflet.js** (open source, sin costo por uso).
- CDN Leaflet: `https://unpkg.com/leaflet@1.9.4/dist/leaflet.js`
- CSS Leaflet: `https://unpkg.com/leaflet@1.9.4/dist/leaflet.css`

### 16.2 Estilo del mapa

- Tile provider: **CartoDB Dark Matter** para el mapa de track & trace principal
  `https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png`
- Tile provider: **CartoDB Positron** para mapas en modales y formularios
  `https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png`

### 16.3 Marcadores por estado

| Estado del vehículo | Color marcador | Ícono interno |
|---|---|---|
| En movimiento | `#00D4FF` (cyan) | Flecha dirección |
| Detenido (normal) | `#1A73E8` (azul) | Punto |
| Detenido (largo) | `#F57F17` (ámbar) | Exclamación |
| Avería / Incidente | `#E53935` (rojo) | X |
| Entregado | `#2E7D32` (verde) | Check |

### 16.4 Rutas en el mapa

- Ruta planificada: línea punteada `#1A73E8`, opacidad 0.6, grosor 2px
- Ruta recorrida (breadcrumb): línea sólida `#00D4FF`, opacidad 0.8, grosor 2px
- Desvío de ruta: línea sólida `#F57F17`, opacidad 0.7, grosor 2px

---

## 17. Responsive y Breakpoints

### 17.1 Breakpoints (Bootstrap 5.3 base)

| Nombre | Desde | Comportamiento |
|---|---|---|
| xs | 0px | No soportado para uso productivo |
| sm | 576px | No soportado para uso productivo |
| md | 768px | Sidebar colapsado automático, KPI 2 columnas |
| lg | 992px | Layout completo, KPI 3 columnas |
| xl | 1280px | **Mínimo soportado para uso productivo** |
| xxl | 1400px | Layout óptimo |

**El sistema Freiroute TMS está diseñado para escritorio (1280px+).** El portal del cliente sí es mobile-first.

### 17.2 Comportamiento en 768px–1279px

- Sidebar: colapsa automáticamente (modo ícono, 64px)
- KPI Grid: 2 columnas
- Tablas: scroll horizontal con `overflow-x: auto`
- Formularios: columnas apiladas (1 columna)

### 17.3 Portal del cliente (móvil)

El módulo **Customer Portal** (EP-11) sí es mobile-first. Usa DM Sans en lugar de Inter, y una paleta simplificada:
- Fondo: `#FFFFFF`
- Acento: `#1A73E8`
- Sin sidebar — navbar top con logo + menú hamburguesa

---

## 18. Variables CSS — Referencia Completa

```css
:root {
  /* ── Identidad ────────────────────────────────────────────── */
  --fr-navy-primary:       #0B2545;
  --fr-navy-mid:           #1B4F8A;
  --fr-navy-light:         #2C6BAD;
  --fr-action-blue:        #1A73E8;
  --fr-action-hover:       #1557B0;
  --fr-cyan-accent:        #00D4FF;
  --fr-blue-tint:          #E3F0FF;

  /* ── Semántica operacional ────────────────────────────────── */
  --fr-success:            #2E7D32;
  --fr-success-light:      #E6F4EA;
  --fr-success-border:     #A5D6A7;
  --fr-warning:            #F57F17;
  --fr-warning-light:      #FFF8E1;
  --fr-warning-border:     #FFE082;
  --fr-danger:             #E53935;
  --fr-danger-light:       #FFEBEE;
  --fr-danger-border:      #EF9A9A;
  --fr-info:               #1A73E8;
  --fr-info-light:         #E3F0FF;

  /* ── Superficies ──────────────────────────────────────────── */
  --fr-surface-bg:         #F8FAFC;
  --fr-surface-card:       #FFFFFF;
  --fr-surface-hover:      #F1F5F9;
  --fr-surface-disabled:   #F8FAFC;
  --fr-border:             #E2E8F0;
  --fr-border-strong:      #CBD5E1;
  --fr-border-focus:       #1A73E8;

  /* ── Texto ────────────────────────────────────────────────── */
  --fr-text-primary:       #1E293B;
  --fr-text-secondary:     #475569;
  --fr-text-muted:         #64748B;
  --fr-text-disabled:      #94A3B8;
  --fr-text-inverse:       #FFFFFF;

  /* ── Sombras ──────────────────────────────────────────────── */
  --fr-shadow-xs:          0 1px 2px rgba(0,0,0,.05);
  --fr-shadow-sm:          0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.06);
  --fr-shadow-md:          0 4px 12px rgba(0,0,0,.10), 0 2px 4px rgba(0,0,0,.06);
  --fr-shadow-lg:          0 10px 25px rgba(0,0,0,.12), 0 4px 6px rgba(0,0,0,.07);

  /* ── Layout ───────────────────────────────────────────────── */
  --fr-sidebar-width:      240px;
  --fr-sidebar-collapsed:  64px;
  --fr-topbar-height:      56px;

  /* ── Espaciado ────────────────────────────────────────────── */
  --fr-space-1:            4px;
  --fr-space-2:            8px;
  --fr-space-3:            12px;
  --fr-space-4:            16px;
  --fr-space-5:            20px;
  --fr-space-6:            24px;
  --fr-space-8:            32px;
  --fr-space-12:           48px;

  /* ── Border radius ────────────────────────────────────────── */
  --fr-radius-xs:          4px;
  --fr-radius-sm:          6px;
  --fr-radius-md:          10px;
  --fr-radius-lg:          14px;
  --fr-radius-full:        9999px;
}
```

---

## 19. Checklist de Conformidad UI

Antes de hacer PR, @FrontendDev verifica:

### Identidad visual
- [ ] Sidebar con fondo `#0B2545` y logo mark cyan
- [ ] Ítem activo del sidebar: fondo `rgba(0,212,255,.12)`, texto `#00D4FF`, borde derecho `2px solid #00D4FF`
- [ ] Topbar blanco con sombra, altura 56px, sticky
- [ ] Fuente Inter en todo el sistema (no Bootstrap default)
- [ ] `ViewData["ActiveMenu"]` asignado correctamente en el controller

### Colores
- [ ] Ningún color hardcodeado en HTML — solo variables `var(--fr-*)`
- [ ] Badges usando exclusivamente clases `fr-badge-*`
- [ ] Color de badges consistente con la tabla de estados (sección 10)
- [ ] KPI values con color semántico según el tipo de dato

### Componentes
- [ ] Tabla usando clases `fr-table`, `fr-table-wrapper`
- [ ] Columna de IDs/códigos con clase `fr-id-code` (JetBrains Mono)
- [ ] Acciones en tabla: orden Ver → Editar → Desactivar
- [ ] Empty state implementado cuando la tabla no tiene datos
- [ ] Paginación con 20 registros y texto "Mostrando X–Y de Z registros"

### Interacción
- [ ] Toasts con `FrToast.success/error/warning` — nunca `alert()`
- [ ] Desactivación con confirmación en JavaScript antes de llamar API
- [ ] Botón de submit deshabilitado durante la petición AJAX
- [ ] Formularios con jQuery Validate configurado y mensajes en español
- [ ] Errores de validación mostrados bajo cada input con clase `fr-form-error`

### Acceso y roles
- [ ] Botón "Crear" protegido con `User.HasPermission("[modulo]", "CREATE")`
- [ ] Botones Editar/Desactivar protegidos con `"UPDATE"`
- [ ] Módulo visible en sidebar solo si el usuario tiene permiso `READ`

### Calidad
- [ ] Sin warnings de consola en navegador
- [ ] Responsivo: tabla con scroll horizontal en < 1024px
- [ ] Imágenes/íconos tienen `title` o `aria-label`
- [ ] Sin colores o estilos de Bootstrap sin pasar por clases `fr-*`

---

*Freiroute Design System v1.0 — 2026*
*Fuente de verdad visual para todos los agentes de IA y desarrolladores del proyecto.*
*Cambios a este documento requieren aprobación de @PM y deben generar un ADR si afectan componentes existentes.*
