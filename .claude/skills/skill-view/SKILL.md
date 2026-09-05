---
description: Design System Freiroute y patrones de vistas Razor MVC. Úsalo para crear vistas con Bootstrap 5.3, implementar el Design System Freiroute (colores, tipografía, componentes), configurar validación cliente con jQuery Validate, y diseñar formularios, tablas paginadas, modales y dashboards KPI.
---

# Skill: View — Design System Freiroute + Vistas Razor MVC

## Design System Freiroute — Variables CSS Completas

```css
/* wwwroot/css/freiroute.css */
:root {
  /* ── Identidad Freiroute ──────────────────────────────── */
  --fr-navy-primary:    #0B2545;   /* Sidebar, navbar, marca */
  --fr-navy-mid:        #1B4F8A;   /* Hover sidebar, gradiente */
  --fr-action-blue:     #1A73E8;   /* Botones CTA, links, acento primario */
  --fr-cyan-accent:     #00D4FF;   /* Logo mark, item activo sidebar */
  --fr-blue-tint:       #E3F0FF;   /* Fondos de tarjetas informativas */

  /* ── Semántica operacional ───────────────────────────── */
  --fr-success:         #2E7D32;   /* Entregado, OTD positivo */
  --fr-success-light:   #E6F4EA;   /* Fondo badge success */
  --fr-warning:         #F57F17;   /* En tránsito, SLA en riesgo */
  --fr-warning-light:   #FFF8E1;   /* Fondo badge warning */
  --fr-danger:          #E53935;   /* Crítico, error, vencido */
  --fr-danger-light:    #FFEBEE;   /* Fondo badge danger */
  --fr-info:            #0891B2;   /* Informativo */
  --fr-info-light:      #E0F7FA;   /* Fondo badge info */

  /* ── Neutrales ───────────────────────────────────────── */
  --fr-surface-bg:      #F8FAFC;   /* Fondo página */
  --fr-surface-card:    #FFFFFF;   /* Tarjetas, modales, paneles */
  --fr-text-primary:    #1E293B;   /* Texto principal */
  --fr-text-secondary:  #64748B;   /* Labels, hints, texto secundario */
  --fr-border:          #E2E8F0;   /* Bordes de tarjetas y tablas */
}
```

## Layout Principal

```
Sidebar expandido:  240px | fondo #0B2545 | texto blanco
Sidebar colapsado:  64px  | solo íconos
Topbar:             56px  | blanco | sombra 0 1px 3px rgba(0,0,0,.08)
Contenido:          calc(100vw - 240px) | padding 24px | fondo #F8FAFC
Cards:              border-radius 10px | border 1px solid #E2E8F0
Tablas:             sin borde externo | border-bottom 1px solid #E2E8F0
Paginado:           20 registros/página
```

## Escala Tipográfica

```css
/* Page Title:    Inter 700 · 28px */
.fr-page-title { font-family: Inter; font-weight: 700; font-size: 28px; color: #1E293B; }

/* Module Title:  Inter 600 · 20px */
.fr-module-title { font-family: Inter; font-weight: 600; font-size: 20px; color: #1E293B; }

/* Card Title:    Inter 600 · 15px */
.fr-card-title { font-family: Inter; font-weight: 600; font-size: 15px; color: #1E293B; }

/* Table Header:  Inter 600 · 11px · UPPERCASE · letter-spacing .05em */
.fr-table-header { font-family: Inter; font-weight: 600; font-size: 11px;
                   color: #64748B; text-transform: uppercase; letter-spacing: .05em; }

/* Body:          Inter 400 · 13px */
body { font-family: Inter; font-weight: 400; font-size: 13px; color: #1E293B; }

/* Code/ID:       JetBrains Mono · 12px */
.fr-code { font-family: 'JetBrains Mono'; font-size: 12px; }
```

## Componentes HTML Estándar

### Badges de Estado Operacional
```html
<span class="badge-fr badge-fr-success">Entregado</span>
<span class="badge-fr badge-fr-info">En tránsito</span>
<span class="badge-fr badge-fr-warning">SLA en riesgo</span>
<span class="badge-fr badge-fr-danger">Retrasado</span>
<span class="badge-fr badge-fr-neutral">Planificado</span>
```

### KPI Cards del Dashboard
```html
<div class="kpi-card">
  <div class="kpi-icon">
    <i class="ti ti-truck text-fr-blue fs-4"></i>
  </div>
  <div class="kpi-content">
    <div class="kpi-label">Embarques hoy</div>
    <div class="kpi-value" style="color: var(--fr-action-blue)">148</div>
    <div class="kpi-delta kpi-up">↑ 12% vs. ayer</div>
  </div>
</div>
```

### Sidebar Navigation
```html
<!-- Sidebar expandido (240px) -->
<nav class="sidebar" style="width: 240px; background: var(--fr-navy-primary);">
  <div class="sidebar-brand">
    <img src="/assets/logo-freiroute.svg" alt="Freiroute TMS" />
  </div>
  <ul class="sidebar-nav">
    <!-- Item activo: background rgba(0,212,255,.12) · texto #00D4FF · border-right 2px #00D4FF -->
    <li>
      <a class="sb-item active" href="/embarques">
        <i class="ti ti-truck"></i>
        <span>Embarques</span>
      </a>
    </li>
    <li>
      <a class="sb-item" href="/ordenes">
        <i class="ti ti-clipboard-list"></i>
        <span>Órdenes</span>
      </a>
    </li>
  </ul>
</nav>
```

## Vistas Razor Estándar

### Index.cshtml — Tabla Paginada
```html
@model PagedResult<[Modulo]ResponseDto>
@{
    ViewData["Title"] = "[Módulo]";
    Layout = "~/Views/Shared/_LayoutTenant.cshtml";
}

<div class="page-header d-flex justify-content-between align-items-center mb-4">
    <h1 class="fr-page-title">[Módulo]</h1>
    @if (User.HasPermission("[modulo]", "CREATE"))
    {
        <a asp-action="Create" class="btn btn-primary btn-fr">
            <i class="ti ti-plus me-1"></i> Nuevo [Entidad]
        </a>
    }
</div>

<!-- Tabla de datos -->
<div class="card fr-card">
    <div class="card-body p-0">
        <table class="table table-hover fr-table mb-0">
            <thead>
                <tr>
                    <th class="fr-table-header">ID</th>
                    <th class="fr-table-header">Nombre</th>
                    <th class="fr-table-header">Estado</th>
                    <th class="fr-table-header">Fecha Creación</th>
                    <th class="fr-table-header text-end">Acciones</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model.Items)
                {
                    <tr>
                        <td class="fr-code">@item.Id.ToString("N").Substring(0, 8).ToUpper()</td>
                        <td>@item.Nombre</td>
                        <td>
                            <span class="badge-fr badge-fr-success">Activo</span>
                        </td>
                        <td>@item.FechaCreacion.ToString("dd/MM/yyyy HH:mm")</td>
                        <td class="text-end">
                            <a asp-action="Edit" asp-route-id="@item.Id"
                               class="btn btn-sm btn-outline-secondary">
                                <i class="ti ti-edit"></i>
                            </a>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
    <!-- Paginación Bootstrap 5 -->
    <div class="card-footer d-flex justify-content-between align-items-center">
        <span class="text-secondary fr-label">
            @Model.TotalCount registros · Página @Model.Page de @Model.TotalPages
        </span>
        <partial name="_Pagination" model="Model" />
    </div>
</div>
```

### Create.cshtml — Formulario con Validación
```html
@model [Modulo]RequestDto
@{
    ViewData["Title"] = "Nuevo [Entidad]";
    Layout = "~/Views/Shared/_LayoutTenant.cshtml";
}

<div class="page-header mb-4">
    <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index">[Módulo]</a></li>
            <li class="breadcrumb-item active">Nuevo</li>
        </ol>
    </nav>
    <h1 class="fr-page-title">Nuevo [Entidad]</h1>
</div>

<div class="card fr-card">
    <div class="card-body">
        <form asp-action="Create" method="post" id="form-crear-[modulo]">
            @Html.AntiForgeryToken()
            <div asp-validation-summary="ModelOnly" class="alert alert-danger"></div>

            <div class="row g-3">
                <div class="col-md-6">
                    <label asp-for="Nombre" class="form-label fr-label">
                        Nombre <span class="text-danger">*</span>
                    </label>
                    <input asp-for="Nombre"
                           class="form-control"
                           placeholder="Nombre del [entidad]"
                           maxlength="200"
                           data-val="true"
                           data-val-required="El nombre es obligatorio."
                           data-val-maxlength="Máximo 200 caracteres."
                           data-val-maxlength-max="200" />
                    <span asp-validation-for="Nombre" class="text-danger small"></span>
                </div>
            </div>

            <div class="d-flex gap-2 mt-4">
                <button type="submit" class="btn btn-primary btn-fr">
                    <i class="ti ti-device-floppy me-1"></i> Guardar
                </button>
                <a asp-action="Index" class="btn btn-outline-secondary">
                    Cancelar
                </a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

## Estados de Embarque → Badges

```csharp
// Helper para determinar badge CSS según estado
public static string GetBadgeClass(string estado) => estado switch
{
    EmbarqueStatus.Draft          => "badge-fr-neutral",
    EmbarqueStatus.Confirmed      => "badge-fr-info",
    EmbarqueStatus.Assigned       => "badge-fr-info",
    EmbarqueStatus.InTransit      => "badge-fr-warning",
    EmbarqueStatus.Delivered      => "badge-fr-success",
    EmbarqueStatus.FailedDelivery => "badge-fr-danger",
    EmbarqueStatus.OnHold         => "badge-fr-warning",
    EmbarqueStatus.Cancelled      => "badge-fr-neutral",
    _ => "badge-fr-neutral"
};
```

## Integración AJAX con ApiResponse<T>

```javascript
// Patrón estándar para llamadas API desde Razor Views
async function cargarDatos(empresaId) {
    try {
        const response = await fetch(`/api/[modulo]`, {
            headers: {
                'Authorization': `Bearer ${getJwtToken()}`,
                'Content-Type': 'application/json'
            }
        });
        const result = await response.json(); // ApiResponse<T>

        if (result.isSuccess) {
            renderizarTabla(result.data);
        } else {
            mostrarError(result.message || 'Error al cargar los datos.');
        }
    } catch (error) {
        mostrarError('Error de conexión. Intente de nuevo.');
        console.error('Error:', error);
    }
}

function mostrarToast(tipo, mensaje) {
    // tipo: 'success' | 'error' | 'warning'
    const toastElement = document.getElementById('toast-notificacion');
    // ... configurar Bootstrap Toast
}
```

## Accesibilidad — Requisitos Mínimos

```html
<!-- Botones de acción con aria-label -->
<button type="button" class="btn btn-sm btn-outline-danger"
        aria-label="Desactivar [Entidad] @item.Nombre">
    <i class="ti ti-trash" aria-hidden="true"></i>
</button>

<!-- Labels siempre asociados con id/for -->
<label for="campo-nombre" class="form-label fr-label">Nombre</label>
<input id="campo-nombre" name="Nombre" ... />

<!-- Roles ARIA en tablas -->
<table class="table fr-table" role="grid" aria-label="Lista de [Módulo]">

<!-- Mensajes de estado accesibles -->
<div role="alert" aria-live="polite" id="mensaje-estado"></div>
```
