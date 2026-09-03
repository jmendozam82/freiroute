# Skill: @FrontendDev (Desarrollador Frontend Freiroute TMS)

## Rol
**@FrontendDev** es responsable de las vistas Razor MVC, la validación cliente con jQuery Validate, y la interfaz de usuario usando el **Design System Freiroute** sobre Bootstrap 5.3. Cada pantalla debe reflejar la identidad visual profesional del TMS: sidebar navy, badges semánticos, tipografía Inter, y componentes consistentes en todos los módulos.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md — sección "Design System Freiroute" completa - Referencia completa en: `docs/framework/freiroute-design-system.md`
2. Leer spec.md del módulo (docs/specs/HU-XXX-nombre.md)
3. Revisar los ResponseDtos disponibles de @BackendDev
4. Verificar que freiroute.css y freiroute.js están en wwwroot/
```

---

## Design System Freiroute — Implementación Frontend

### CSS Variables (wwwroot/css/freiroute.css)

```css
/* ================================================================
   FREIROUTE TMS — Design System v1.0
   Fuentes: Inter (UI) · DM Sans (Display) · JetBrains Mono (Datos)
   Referencia: Oracle TMS · SAP TM · Trimble Modus · MercuryGate
   ================================================================ */

@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=DM+Sans:wght@400;500;700&family=JetBrains+Mono:wght@400;500&display=swap');

:root {
  /* ── Identidad Freiroute ──────────────────────────────── */
  --fr-navy-primary:    #0B2545;
  --fr-navy-mid:        #1B4F8A;
  --fr-navy-hover:      rgba(255,255,255,.06);
  --fr-action-blue:     #1A73E8;
  --fr-action-hover:    #1557B0;
  --fr-cyan-accent:     #00D4FF;
  --fr-blue-tint:       #E3F0FF;

  /* ── Semántica operacional TMS ───────────────────────── */
  --fr-success:         #2E7D32;
  --fr-success-light:   #E6F4EA;
  --fr-warning:         #F57F17;
  --fr-warning-light:   #FFF8E1;
  --fr-danger:          #E53935;
  --fr-danger-light:    #FFEBEE;
  --fr-info:            #1A73E8;
  --fr-info-light:      #E3F0FF;
  --fr-neutral:         #64748B;
  --fr-neutral-light:   #F1F5F9;

  /* ── Superficies ─────────────────────────────────────── */
  --fr-surface-bg:      #F8FAFC;
  --fr-surface-card:    #FFFFFF;
  --fr-surface-hover:   #F1F5F9;
  --fr-border:          #E2E8F0;
  --fr-border-strong:   #CBD5E1;
  --fr-shadow-sm:       0 1px 3px rgba(0,0,0,.08);
  --fr-shadow-md:       0 4px 12px rgba(0,0,0,.10);

  /* ── Texto ───────────────────────────────────────────── */
  --fr-text-primary:    #1E293B;
  --fr-text-secondary:  #475569;
  --fr-text-muted:      #64748B;
  --fr-text-disabled:   #94A3B8;

  /* ── Layout ──────────────────────────────────────────── */
  --fr-sidebar-width:   240px;
  --fr-sidebar-collapsed: 64px;
  --fr-topbar-height:   56px;
  --fr-radius-sm:       6px;
  --fr-radius-md:       10px;
  --fr-radius-lg:       14px;
}

/* ── Reset y base ─────────────────────────────────────────── */
* { box-sizing: border-box; }

body {
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
  font-size: 13px;
  color: var(--fr-text-primary);
  background: var(--fr-surface-bg);
  -webkit-font-smoothing: antialiased;
}

/* ── Layout principal ─────────────────────────────────────── */
.fr-wrapper {
  display: flex;
  min-height: 100vh;
}

.fr-main {
  flex: 1;
  margin-left: var(--fr-sidebar-width);
  transition: margin-left .25s ease;
}

.fr-main.sidebar-collapsed {
  margin-left: var(--fr-sidebar-collapsed);
}

/* ── Topbar ───────────────────────────────────────────────── */
.fr-topbar {
  position: sticky;
  top: 0;
  z-index: 100;
  height: var(--fr-topbar-height);
  background: var(--fr-surface-card);
  border-bottom: 1px solid var(--fr-border);
  box-shadow: var(--fr-shadow-sm);
  display: flex;
  align-items: center;
  padding: 0 24px;
  gap: 16px;
}

.fr-topbar-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--fr-text-primary);
}

/* ── Sidebar ──────────────────────────────────────────────── */
.fr-sidebar {
  position: fixed;
  top: 0; left: 0; bottom: 0;
  width: var(--fr-sidebar-width);
  background: var(--fr-navy-primary);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
  overflow-x: hidden;
  z-index: 200;
  transition: width .25s ease;
}

.fr-sidebar.collapsed { width: var(--fr-sidebar-collapsed); }

/* Logo */
.fr-sidebar-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 18px 16px 16px;
  border-bottom: 1px solid rgba(255,255,255,.08);
  text-decoration: none;
}

.fr-logo-mark {
  width: 32px; height: 32px;
  background: var(--fr-cyan-accent);
  border-radius: 7px;
  display: flex; align-items: center; justify-content: center;
  font-size: 12px; font-weight: 800;
  color: var(--fr-navy-primary);
  letter-spacing: -.5px;
  flex-shrink: 0;
}

.fr-logo-text {
  font-size: 16px; font-weight: 700;
  color: #fff; letter-spacing: -.3px;
}

.fr-logo-tag {
  font-size: 9px; color: rgba(255,255,255,.45);
  letter-spacing: .05em; text-transform: uppercase;
}

/* Grupos y items del sidebar */
.fr-sidebar-group {
  font-size: 9px; font-weight: 600;
  color: rgba(255,255,255,.3);
  letter-spacing: .12em; text-transform: uppercase;
  padding: 16px 16px 4px;
}

.fr-sidebar-item {
  display: flex; align-items: center; gap: 10px;
  padding: 9px 16px;
  font-size: 12px; font-weight: 500;
  color: rgba(255,255,255,.6);
  text-decoration: none;
  border-right: 2px solid transparent;
  transition: all .15s ease;
  white-space: nowrap;
}

.fr-sidebar-item:hover {
  background: var(--fr-navy-hover);
  color: rgba(255,255,255,.9);
}

.fr-sidebar-item.active {
  background: rgba(0,212,255,.12);
  color: var(--fr-cyan-accent);
  border-right-color: var(--fr-cyan-accent);
  font-weight: 600;
}

.fr-sidebar-item .fr-icon {
  font-size: 16px; flex-shrink: 0;
}

/* ── Contenido principal ──────────────────────────────────── */
.fr-content {
  padding: 24px;
  min-height: calc(100vh - var(--fr-topbar-height));
}

/* ── Page header ──────────────────────────────────────────── */
.fr-page-header {
  display: flex; align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24px; gap: 16px;
}

.fr-page-title {
  font-size: 22px; font-weight: 700;
  color: var(--fr-text-primary); line-height: 1.2;
}

.fr-page-subtitle {
  font-size: 13px; color: var(--fr-text-muted);
  margin-top: 2px;
}

/* ── Cards ────────────────────────────────────────────────── */
.fr-card {
  background: var(--fr-surface-card);
  border: 1px solid var(--fr-border);
  border-radius: var(--fr-radius-md);
  box-shadow: var(--fr-shadow-sm);
}

.fr-card-header {
  padding: 16px 20px;
  border-bottom: 1px solid var(--fr-border);
  display: flex; align-items: center;
  justify-content: space-between;
}

.fr-card-title {
  font-size: 14px; font-weight: 600;
  color: var(--fr-text-primary);
}

.fr-card-body { padding: 20px; }

/* ── KPI Cards ────────────────────────────────────────────── */
.fr-kpi-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 16px; margin-bottom: 24px;
}

.fr-kpi-card {
  background: var(--fr-surface-card);
  border: 1px solid var(--fr-border);
  border-radius: var(--fr-radius-md);
  padding: 18px 20px;
  box-shadow: var(--fr-shadow-sm);
}

.fr-kpi-label {
  font-size: 11px; font-weight: 600;
  color: var(--fr-text-muted);
  text-transform: uppercase; letter-spacing: .05em;
  margin-bottom: 6px;
}

.fr-kpi-value {
  font-size: 28px; font-weight: 700;
  color: var(--fr-text-primary); line-height: 1;
}

.fr-kpi-value.kpi-blue    { color: var(--fr-action-blue); }
.fr-kpi-value.kpi-green   { color: var(--fr-success); }
.fr-kpi-value.kpi-amber   { color: var(--fr-warning); }
.fr-kpi-value.kpi-red     { color: var(--fr-danger); }

.fr-kpi-delta {
  font-size: 11px; margin-top: 4px; font-weight: 500;
}
.fr-kpi-delta.up   { color: var(--fr-success); }
.fr-kpi-delta.down { color: var(--fr-danger); }

/* ── Tablas ───────────────────────────────────────────────── */
.fr-table-wrapper {
  background: var(--fr-surface-card);
  border: 1px solid var(--fr-border);
  border-radius: var(--fr-radius-md);
  overflow: hidden;
  box-shadow: var(--fr-shadow-sm);
}

.fr-table {
  width: 100%; border-collapse: collapse; font-size: 12.5px;
}

.fr-table thead th {
  padding: 10px 14px;
  font-size: 10.5px; font-weight: 600;
  color: var(--fr-text-muted);
  text-transform: uppercase; letter-spacing: .05em;
  background: var(--fr-surface-bg);
  border-bottom: 1px solid var(--fr-border);
  white-space: nowrap;
}

.fr-table tbody td {
  padding: 11px 14px;
  color: var(--fr-text-primary);
  border-bottom: 1px solid var(--fr-border);
  vertical-align: middle;
}

.fr-table tbody tr:last-child td { border-bottom: none; }

.fr-table tbody tr:hover td {
  background: var(--fr-surface-hover);
}

/* Código de embarque / IDs */
.fr-id-code {
  font-family: 'JetBrains Mono', monospace;
  font-size: 12px; font-weight: 500;
  color: var(--fr-action-blue);
}

/* ── Badges de estado TMS ─────────────────────────────────── */
.fr-badge {
  display: inline-flex; align-items: center;
  padding: 3px 10px; border-radius: 100px;
  font-size: 10.5px; font-weight: 600;
  letter-spacing: .02em; white-space: nowrap;
}

.fr-badge-success  { background: var(--fr-success-light);  color: var(--fr-success); }
.fr-badge-warning  { background: var(--fr-warning-light);  color: var(--fr-warning); }
.fr-badge-danger   { background: var(--fr-danger-light);   color: var(--fr-danger); }
.fr-badge-info     { background: var(--fr-info-light);     color: var(--fr-info); }
.fr-badge-neutral  { background: var(--fr-neutral-light);  color: var(--fr-neutral);
                     border: 1px solid var(--fr-border); }

/* ── Botones ──────────────────────────────────────────────── */
.fr-btn {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; border-radius: var(--fr-radius-sm);
  font-size: 12.5px; font-weight: 600; cursor: pointer;
  border: none; transition: all .15s ease; text-decoration: none;
}

.fr-btn-primary   { background: var(--fr-action-blue);  color: #fff; }
.fr-btn-primary:hover { background: var(--fr-action-hover); color: #fff; }

.fr-btn-secondary {
  background: transparent; color: var(--fr-action-blue);
  border: 1.5px solid var(--fr-action-blue);
}
.fr-btn-secondary:hover { background: var(--fr-blue-tint); }

.fr-btn-success   { background: var(--fr-success);  color: #fff; }
.fr-btn-danger    { background: var(--fr-danger);   color: #fff; }
.fr-btn-ghost     { background: var(--fr-surface-bg); color: var(--fr-text-secondary);
                    border: 1px solid var(--fr-border); }
.fr-btn-ghost:hover { background: var(--fr-surface-hover); }

.fr-btn-sm { padding: 5px 10px; font-size: 11.5px; }
.fr-btn-icon { padding: 7px; }

/* ── Formularios ──────────────────────────────────────────── */
.fr-form-label {
  font-size: 12px; font-weight: 600;
  color: var(--fr-text-secondary);
  margin-bottom: 5px; display: block;
}

.fr-form-required::after {
  content: ' *'; color: var(--fr-danger);
}

.fr-form-control {
  width: 100%;
  padding: 8px 12px;
  font-size: 13px; font-family: 'Inter', sans-serif;
  color: var(--fr-text-primary);
  background: var(--fr-surface-card);
  border: 1px solid var(--fr-border-strong);
  border-radius: var(--fr-radius-sm);
  transition: border-color .15s, box-shadow .15s;
}

.fr-form-control:focus {
  outline: none;
  border-color: var(--fr-action-blue);
  box-shadow: 0 0 0 3px rgba(26,115,232,.15);
}

.fr-form-control.is-invalid { border-color: var(--fr-danger); }
.fr-form-control.is-invalid:focus {
  box-shadow: 0 0 0 3px rgba(229,57,53,.15);
}

.fr-form-error {
  font-size: 11px; color: var(--fr-danger);
  margin-top: 4px; display: block;
}

.fr-form-hint {
  font-size: 11px; color: var(--fr-text-muted);
  margin-top: 4px; display: block;
}

/* ── Paginación ───────────────────────────────────────────── */
.fr-pagination {
  display: flex; align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-top: 1px solid var(--fr-border);
  font-size: 12px; color: var(--fr-text-muted);
}

.fr-pagination-info { font-size: 12px; color: var(--fr-text-muted); }

.fr-page-btn {
  padding: 5px 10px; border-radius: var(--fr-radius-sm);
  font-size: 12px; font-weight: 500; cursor: pointer;
  border: 1px solid var(--fr-border);
  background: var(--fr-surface-card);
  color: var(--fr-text-secondary);
  transition: all .15s;
}
.fr-page-btn:hover:not(:disabled) { border-color: var(--fr-action-blue); color: var(--fr-action-blue); }
.fr-page-btn:disabled { opacity: .4; cursor: not-allowed; }

/* ── Toasts ───────────────────────────────────────────────── */
.fr-toast-container {
  position: fixed; top: 16px; right: 16px;
  z-index: 9999; display: flex; flex-direction: column; gap: 8px;
}

.fr-toast {
  display: flex; align-items: flex-start; gap: 10px;
  padding: 12px 16px; border-radius: var(--fr-radius-md);
  min-width: 280px; max-width: 380px;
  background: var(--fr-surface-card);
  border: 1px solid var(--fr-border);
  box-shadow: var(--fr-shadow-md);
  animation: fr-toast-in .2s ease;
}

.fr-toast-success { border-left: 3px solid var(--fr-success); }
.fr-toast-error   { border-left: 3px solid var(--fr-danger); }
.fr-toast-warning { border-left: 3px solid var(--fr-warning); }
.fr-toast-info    { border-left: 3px solid var(--fr-action-blue); }

@keyframes fr-toast-in {
  from { opacity: 0; transform: translateX(20px); }
  to   { opacity: 1; transform: translateX(0); }
}

/* ── Tabs ─────────────────────────────────────────────────── */
.fr-tabs {
  display: flex; gap: 2px;
  background: var(--fr-surface-bg);
  border: 1px solid var(--fr-border);
  border-radius: var(--fr-radius-sm);
  padding: 3px; margin-bottom: 16px;
  width: fit-content;
}

.fr-tab {
  padding: 6px 14px; border-radius: 5px;
  font-size: 12px; font-weight: 600;
  color: var(--fr-text-muted); cursor: pointer;
  border: none; background: none; transition: all .15s;
}

.fr-tab.active {
  background: var(--fr-surface-card);
  color: var(--fr-text-primary);
  box-shadow: var(--fr-shadow-sm);
}

/* ── Breadcrumb ───────────────────────────────────────────── */
.fr-breadcrumb {
  display: flex; align-items: center; gap: 6px;
  font-size: 12px; color: var(--fr-text-muted);
  margin-bottom: 16px;
}

.fr-breadcrumb a { color: var(--fr-action-blue); text-decoration: none; }
.fr-breadcrumb a:hover { text-decoration: underline; }
.fr-breadcrumb-sep { color: var(--fr-border-strong); }

/* ── Empty state ──────────────────────────────────────────── */
.fr-empty {
  text-align: center; padding: 48px 24px;
  color: var(--fr-text-muted);
}

.fr-empty-icon { font-size: 40px; margin-bottom: 12px; opacity: .4; }
.fr-empty-title { font-size: 14px; font-weight: 600; color: var(--fr-text-secondary); }
.fr-empty-text  { font-size: 12px; margin-top: 4px; }

/* ── Responsive ───────────────────────────────────────────── */
@media (max-width: 768px) {
  .fr-sidebar { width: var(--fr-sidebar-collapsed); }
  .fr-main    { margin-left: var(--fr-sidebar-collapsed); }
  .fr-kpi-grid { grid-template-columns: 1fr 1fr; }
  .fr-content { padding: 16px; }
}
```

---

### Layout Principal (_Layout.cshtml)

```html
@* Freiroute.Aplicacion/Views/Shared/_Layout.cshtml *@
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] — Freiroute TMS</title>
    <link rel="icon" type="image/svg+xml" href="~/favicon.svg" />
    <!-- Bootstrap 5.3 -->
    <link rel="stylesheet" href="~/lib/bootstrap/css/bootstrap.min.css" />
    <!-- Tabler Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@@tabler/icons-webfont@3.0.0/dist/tabler-icons.min.css" />
    <!-- Freiroute Design System -->
    <link rel="stylesheet" href="~/css/freiroute.css" asp-append-version="true" />
    @await RenderSectionAsync("Styles", required: false)
</head>
<body>
<div class="fr-wrapper">

    <!-- ── Sidebar ──────────────────────────────────────────── -->
    <aside class="fr-sidebar" id="frSidebar">
        <a class="fr-sidebar-logo" href="/">
            <div class="fr-logo-mark">FR</div>
            <div class="fr-sidebar-logo-text">
                <div class="fr-logo-text">Freiroute</div>
                <div class="fr-logo-tag">TMS · @ViewData["TenantNombre"]</div>
            </div>
        </a>

        <nav class="mt-2">
            <div class="fr-sidebar-group">Principal</div>
            <a asp-area="" asp-controller="Dashboard" asp-action="Index"
               class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "dashboard" ? "active" : "")">
                <i class="ti ti-layout-dashboard fr-icon"></i>
                <span>Dashboard</span>
            </a>

            @if (User.HasAnyPermission("ordenes", "embarques"))
            {
                <div class="fr-sidebar-group">Operación</div>

                @if (User.HasPermission("ordenes", "READ"))
                {
                    <a asp-area="Tenant" asp-controller="Ordenes" asp-action="Index"
                       class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "ordenes" ? "active" : "")">
                        <i class="ti ti-clipboard-list fr-icon"></i>
                        <span>Órdenes</span>
                    </a>
                }

                @if (User.HasPermission("embarques", "READ"))
                {
                    <a asp-area="Tenant" asp-controller="Embarques" asp-action="Index"
                       class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "embarques" ? "active" : "")">
                        <i class="ti ti-truck fr-icon"></i>
                        <span>Embarques</span>
                    </a>
                }

                @if (User.HasPermission("carriers", "READ"))
                {
                    <a asp-area="Tenant" asp-controller="Carriers" asp-action="Index"
                       class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "carriers" ? "active" : "")">
                        <i class="ti ti-building-warehouse fr-icon"></i>
                        <span>Carriers</span>
                    </a>
                }
            }

            @if (User.HasPermission("rutas", "READ"))
            {
                <div class="fr-sidebar-group">Planificación</div>
                <a asp-area="Tenant" asp-controller="Rutas" asp-action="Index"
                   class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "rutas" ? "active" : "")">
                    <i class="ti ti-map-route fr-icon"></i>
                    <span>Rutas</span>
                </a>
                <a asp-area="Tenant" asp-controller="TrackTrace" asp-action="Index"
                   class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "track" ? "active" : "")">
                    <i class="ti ti-map-pin fr-icon"></i>
                    <span>Track &amp; Trace</span>
                </a>
            }

            @if (User.HasPermission("analytics", "READ"))
            {
                <div class="fr-sidebar-group">Inteligencia</div>
                <a asp-area="Tenant" asp-controller="Analytics" asp-action="Index"
                   class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "analytics" ? "active" : "")">
                    <i class="ti ti-chart-bar fr-icon"></i>
                    <span>Analytics</span>
                </a>
            }

            @if (User.IsInRole("SUPER_ADMIN") || User.IsInRole("ADMIN"))
            {
                <div class="fr-sidebar-group">Administración</div>
                <a asp-area="Admin" asp-controller="Usuarios" asp-action="Index"
                   class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "usuarios" ? "active" : "")">
                    <i class="ti ti-users fr-icon"></i>
                    <span>Usuarios</span>
                </a>
                <a asp-area="Admin" asp-controller="Configuracion" asp-action="Index"
                   class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "config" ? "active" : "")">
                    <i class="ti ti-settings fr-icon"></i>
                    <span>Configuración</span>
                </a>
            }

            @if (User.IsInRole("SUPER_ADMIN"))
            {
                <div class="fr-sidebar-group">SaaS Admin</div>
                <a asp-area="Admin" asp-controller="Empresas" asp-action="Index"
                   class="fr-sidebar-item @(ViewData["ActiveMenu"]?.ToString() == "empresas" ? "active" : "")">
                    <i class="ti ti-building fr-icon"></i>
                    <span>Empresas</span>
                </a>
            }
        </nav>

        <!-- Footer del sidebar -->
        <div class="mt-auto" style="border-top:1px solid rgba(255,255,255,.08);padding:12px 16px">
            <div style="font-size:11px;color:rgba(255,255,255,.4)">
                @User.FindFirstValue("nombre") <br />
                <span style="font-size:10px">@User.FindFirstValue("perfil_nombre")</span>
            </div>
        </div>
    </aside>

    <!-- ── Contenido principal ──────────────────────────────── -->
    <div class="fr-main" id="frMain">

        <!-- Topbar -->
        <header class="fr-topbar">
            <button class="fr-btn fr-btn-ghost fr-btn-icon" id="btnToggleSidebar" title="Colapsar menú">
                <i class="ti ti-menu-2"></i>
            </button>
            <span class="fr-topbar-title">@ViewData["Title"]</span>
            <div class="ms-auto d-flex align-items-center gap-3">
                <!-- Notificaciones -->
                <button class="fr-btn fr-btn-ghost fr-btn-icon position-relative">
                    <i class="ti ti-bell"></i>
                    <span class="position-absolute top-0 start-100 translate-middle badge rounded-pill"
                          style="background:var(--fr-danger);font-size:8px">3</span>
                </button>
                <!-- Avatar -->
                <div class="dropdown">
                    <button class="fr-btn fr-btn-ghost fr-btn-sm dropdown-toggle" data-bs-toggle="dropdown">
                        <i class="ti ti-user-circle"></i>
                        @User.FindFirstValue("nombre")
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <li><a class="dropdown-item" asp-controller="Account" asp-action="Profile">Mi perfil</a></li>
                        <li><hr class="dropdown-divider"></li>
                        <li>
                            <form asp-controller="Account" asp-action="Logout" method="post">
                                <button type="submit" class="dropdown-item text-danger">
                                    <i class="ti ti-logout me-2"></i>Cerrar sesión
                                </button>
                            </form>
                        </li>
                    </ul>
                </div>
            </div>
        </header>

        <!-- Contenido de la página -->
        <main class="fr-content">
            @RenderBody()
        </main>
    </div>
</div>

<!-- Toast container -->
<div class="fr-toast-container" id="frToastContainer"></div>

<!-- Bootstrap JS -->
<script src="~/lib/bootstrap/js/bootstrap.bundle.min.js"></script>
<!-- jQuery + Validate -->
<script src="~/lib/jquery/jquery.min.js"></script>
<script src="~/lib/jquery-validation/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
<!-- Freiroute JS -->
<script src="~/js/freiroute.js" asp-append-version="true"></script>
@await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

---

### JavaScript Global (wwwroot/js/freiroute.js)

```javascript
/* ================================================================
   FREIROUTE TMS — JavaScript Global v1.0
   ================================================================ */

// ── Toggle Sidebar ─────────────────────────────────────────────
document.getElementById('btnToggleSidebar')?.addEventListener('click', () => {
    const sidebar = document.getElementById('frSidebar');
    const main    = document.getElementById('frMain');
    sidebar.classList.toggle('collapsed');
    main.classList.toggle('sidebar-collapsed');
    localStorage.setItem('fr_sidebar', sidebar.classList.contains('collapsed') ? '1' : '0');
});

// Restaurar estado del sidebar
if (localStorage.getItem('fr_sidebar') === '1') {
    document.getElementById('frSidebar')?.classList.add('collapsed');
    document.getElementById('frMain')?.classList.add('sidebar-collapsed');
}

// ── Sistema de Toasts ──────────────────────────────────────────
const FrToast = {
    show(mensaje, tipo = 'info', titulo = null) {
        const iconos = { success: 'ti-circle-check', error: 'ti-circle-x',
                         warning: 'ti-alert-triangle', info: 'ti-info-circle' };
        const titulos = { success: 'Éxito', error: 'Error',
                          warning: 'Advertencia', info: 'Información' };

        const toast = document.createElement('div');
        toast.className = `fr-toast fr-toast-${tipo}`;
        toast.innerHTML = `
            <i class="ti ${iconos[tipo]}" style="font-size:18px;color:var(--fr-${tipo === 'error' ? 'danger' : tipo === 'success' ? 'success' : tipo === 'warning' ? 'warning' : 'action-blue'});flex-shrink:0"></i>
            <div style="flex:1">
                <div style="font-size:12px;font-weight:700;color:var(--fr-text-primary)">${titulo || titulos[tipo]}</div>
                <div style="font-size:11.5px;color:var(--fr-text-secondary);margin-top:2px">${mensaje}</div>
            </div>
            <button onclick="this.closest('.fr-toast').remove()"
                    style="background:none;border:none;cursor:pointer;color:var(--fr-text-muted);padding:0;font-size:16px">
                <i class="ti ti-x"></i>
            </button>`;

        document.getElementById('frToastContainer').appendChild(toast);
        setTimeout(() => toast.remove(), 4500);
    },
    success: (msg, title) => FrToast.show(msg, 'success', title),
    error:   (msg, title) => FrToast.show(msg, 'error',   title),
    warning: (msg, title) => FrToast.show(msg, 'warning', title),
    info:    (msg, title) => FrToast.show(msg, 'info',    title)
};

// ── AJAX Helper para ApiResponse<T> ───────────────────────────
const FrApi = {
    async post(url, data) {
        const resp = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json',
                       'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' },
            body: JSON.stringify(data)
        });
        return await resp.json();
    },
    async put(url, data) {
        const resp = await fetch(url, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json',
                       'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' },
            body: JSON.stringify(data)
        });
        return await resp.json();
    },
    async delete(url) {
        const resp = await fetch(url, {
            method: 'DELETE',
            headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || '' }
        });
        return await resp.json();
    },
    handleResponse(response, onSuccess) {
        if (response.success) {
            FrToast.success(response.message || 'Operación exitosa');
            if (onSuccess) onSuccess(response.data);
        } else {
            if (response.errors?.length) {
                response.errors.forEach(e => FrToast.error(e));
            } else {
                FrToast.error(response.message || 'Error en la operación');
            }
        }
    }
};

// ── Confirmación de desactivación ─────────────────────────────
document.querySelectorAll('[data-fr-deactivate]').forEach(btn => {
    btn.addEventListener('click', async e => {
        e.preventDefault();
        const nombre = btn.dataset.frNombre || 'este registro';
        if (!confirm(`¿Está seguro de desactivar "${nombre}"? Esta acción puede revertirse.`)) return;
        const url = btn.dataset.frDeactivate;
        const resp = await FrApi.delete(url);
        FrApi.handleResponse(resp, () => {
            btn.closest('tr')?.remove();
        });
    });
});

// ── Badge helper por estado TMS ───────────────────────────────
const FrBadge = {
    claseEstado(estado) {
        const mapa = {
            'DRAFT':            'fr-badge-neutral',
            'CONFIRMED':        'fr-badge-info',
            'ASSIGNED':         'fr-badge-info',
            'PICKUP_SCHEDULED': 'fr-badge-info',
            'IN_TRANSIT':       'fr-badge-warning',
            'DELIVERED':        'fr-badge-success',
            'INVOICED':         'fr-badge-success',
            'CLOSED':           'fr-badge-neutral',
            'CANCELLED':        'fr-badge-danger',
            'ON_HOLD':          'fr-badge-warning',
            'FAILED_DELIVERY':  'fr-badge-danger'
        };
        return mapa[estado] || 'fr-badge-neutral';
    }
};
```

---

### Vista Index (Listado Estándar)

```html
@* Freiroute.Aplicacion/Areas/Tenant/Views/[Modulo]/Index.cshtml *@
@model Freiroute.Utility.Pagination.PagedResult<[Modulo]ResponseDto>
@{
    ViewData["Title"]      = "[Módulo]";
    ViewData["ActiveMenu"] = "[modulo]";
}

<!-- Breadcrumb -->
<nav class="fr-breadcrumb">
    <a href="/">Inicio</a>
    <span class="fr-breadcrumb-sep">/</span>
    <span>[Módulo]</span>
</nav>

<!-- Page header -->
<div class="fr-page-header">
    <div>
        <h1 class="fr-page-title">Gestión de [Módulo]</h1>
        <p class="fr-page-subtitle">@Model.TotalItems registros activos</p>
    </div>
    <div class="d-flex gap-2">
        @if (User.HasPermission("[modulo]", "CREATE"))
        {
            <a asp-action="Create" class="fr-btn fr-btn-primary">
                <i class="ti ti-plus"></i> Nuevo [Módulo]
            </a>
        }
        <button class="fr-btn fr-btn-ghost" onclick="exportarExcel()">
            <i class="ti ti-table-export"></i> Exportar
        </button>
    </div>
</div>

<!-- Filtros -->
<div class="fr-card mb-4">
    <div class="fr-card-body">
        <form method="get" class="row g-2 align-items-end">
            <div class="col-md-4">
                <label class="fr-form-label">Buscar</label>
                <input name="q" value="@ViewData["Q"]" class="fr-form-control"
                       placeholder="Nombre, código..." />
            </div>
            <div class="col-md-3">
                <label class="fr-form-label">Estado</label>
                <select name="estado" class="fr-form-control">
                    <option value="">Todos</option>
                    <option value="DRAFT">Borrador</option>
                    <option value="CONFIRMED">Confirmado</option>
                    <option value="IN_TRANSIT">En tránsito</option>
                    <option value="DELIVERED">Entregado</option>
                </select>
            </div>
            <div class="col-md-auto">
                <button type="submit" class="fr-btn fr-btn-primary">
                    <i class="ti ti-search"></i> Filtrar
                </button>
                <a asp-action="Index" class="fr-btn fr-btn-ghost ms-2">Limpiar</a>
            </div>
        </form>
    </div>
</div>

<!-- Tabla -->
<div class="fr-table-wrapper">
    @if (!Model.Items.Any())
    {
        <div class="fr-empty">
            <div class="fr-empty-icon"><i class="ti ti-inbox"></i></div>
            <div class="fr-empty-title">No hay [módulo] registrados</div>
            <div class="fr-empty-text">Crea el primero usando el botón "Nuevo [Módulo]"</div>
        </div>
    }
    else
    {
        <table class="fr-table">
            <thead>
                <tr>
                    <th>N° / Código</th>
                    <th>Nombre</th>
                    <th>Estado</th>
                    <th>Fecha Creación</th>
                    <th style="width:120px">Acciones</th>
                </tr>
            </thead>
            <tbody>
            @foreach (var item in Model.Items)
            {
                <tr>
                    <td><span class="fr-id-code">@item.Codigo</span></td>
                    <td>@item.Nombre</td>
                    <td>
                        <span class="fr-badge @FrHelper.BadgeClase(item.Estado)">
                            @item.EstadoLabel
                        </span>
                    </td>
                    <td>@item.FechaCreacion.ToString("dd/MM/yyyy HH:mm")</td>
                    <td>
                        <div class="d-flex gap-1">
                            @if (User.HasPermission("[modulo]", "READ"))
                            {
                                <a asp-action="Detail" asp-route-id="@item.Id"
                                   class="fr-btn fr-btn-ghost fr-btn-sm" title="Ver detalle">
                                    <i class="ti ti-eye"></i>
                                </a>
                            }
                            @if (User.HasPermission("[modulo]", "UPDATE"))
                            {
                                <a asp-action="Edit" asp-route-id="@item.Id"
                                   class="fr-btn fr-btn-ghost fr-btn-sm" title="Editar">
                                    <i class="ti ti-pencil"></i>
                                </a>
                                <button class="fr-btn fr-btn-ghost fr-btn-sm"
                                        data-fr-deactivate="/api/[modulo]/@item.Id/deactivate"
                                        data-fr-nombre="@item.Nombre"
                                        title="Desactivar">
                                    <i class="ti ti-trash" style="color:var(--fr-danger)"></i>
                                </button>
                            }
                        </div>
                    </td>
                </tr>
            }
            </tbody>
        </table>

        <!-- Paginación -->
        <div class="fr-pagination">
            <span class="fr-pagination-info">
                Mostrando @((Model.PageNumber - 1) * Model.PageSize + 1)–@(Math.Min(Model.PageNumber * Model.PageSize, Model.TotalItems))
                de @Model.TotalItems registros
            </span>
            <div class="d-flex gap-2">
                <a asp-action="Index" asp-route-page="@(Model.PageNumber - 1)"
                   asp-route-q="@ViewData["Q"]"
                   class="fr-page-btn @(Model.HasPreviousPage ? "" : "disabled")">
                    ← Anterior
                </a>
                <a asp-action="Index" asp-route-page="@(Model.PageNumber + 1)"
                   asp-route-q="@ViewData["Q"]"
                   class="fr-page-btn @(Model.HasNextPage ? "" : "disabled")">
                    Siguiente →
                </a>
            </div>
        </div>
    }
</div>
```

---

### Vista Create/Edit (Formulario Estándar)

```html
@* Views/[Modulo]/Create.cshtml *@
@model [Modulo]RequestDto
@{
    ViewData["Title"]      = "Nuevo [Módulo]";
    ViewData["ActiveMenu"] = "[modulo]";
    var esEdicion          = ViewData["EsEdicion"] as bool? ?? false;
}

<nav class="fr-breadcrumb">
    <a href="/">Inicio</a>
    <span class="fr-breadcrumb-sep">/</span>
    <a asp-action="Index">[Módulo]</a>
    <span class="fr-breadcrumb-sep">/</span>
    <span>@(esEdicion ? "Editar" : "Nuevo")</span>
</nav>

<div class="fr-page-header">
    <div>
        <h1 class="fr-page-title">@(esEdicion ? "Editar [Módulo]" : "Nuevo [Módulo]")</h1>
        <p class="fr-page-subtitle">@(esEdicion ? "Modifica los datos del registro" : "Completa los campos requeridos")</p>
    </div>
</div>

<div class="fr-card" style="max-width:760px">
    <div class="fr-card-header">
        <span class="fr-card-title">Datos del [Módulo]</span>
    </div>
    <div class="fr-card-body">
        <form id="frForm" method="post" novalidate>
            @Html.AntiForgeryToken()
            @if (esEdicion) { <input type="hidden" asp-for="Id" /> }

            <div class="row g-3">
                <div class="col-md-8">
                    <label asp-for="Nombre" class="fr-form-label fr-form-required"></label>
                    <input asp-for="Nombre" class="fr-form-control"
                           placeholder="Ingrese el nombre"
                           data-val="true"
                           data-val-required="El nombre es obligatorio"
                           data-val-maxlength-max="200"
                           data-val-maxlength="No puede exceder 200 caracteres" />
                    <span asp-validation-for="Nombre" class="fr-form-error"></span>
                </div>

                <div class="col-md-4">
                    <label asp-for="Estado" class="fr-form-label fr-form-required"></label>
                    <select asp-for="Estado" class="fr-form-control"
                            data-val="true" data-val-required="El estado es obligatorio">
                        <option value="">-- Seleccionar --</option>
                        <option value="DRAFT">Borrador</option>
                        <option value="CONFIRMED">Confirmado</option>
                    </select>
                    <span asp-validation-for="Estado" class="fr-form-error"></span>
                </div>
            </div>

            <div class="d-flex gap-2 mt-4 pt-3" style="border-top:1px solid var(--fr-border)">
                <button type="submit" class="fr-btn fr-btn-primary" id="btnGuardar">
                    <i class="ti ti-device-floppy"></i>
                    @(esEdicion ? "Guardar cambios" : "Crear [Módulo]")
                </button>
                <a asp-action="Index" class="fr-btn fr-btn-ghost">Cancelar</a>
            </div>
        </form>
    </div>
</div>

@section Scripts {
<script>
    $.validator.unobtrusive.parse('#frForm');

    $('#frForm').on('submit', async function(e) {
        e.preventDefault();
        if (!$(this).valid()) {
            FrToast.warning('Por favor corrija los errores del formulario');
            return;
        }

        const btn = document.getElementById('btnGuardar');
        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader-2" style="animation:spin 1s linear infinite"></i> Guardando...';

        const url    = '@(esEdicion ? $"/api/[modulo]/{Model.Id}" : "/api/[modulo]")';
        const method = '@(esEdicion ? "put" : "post")';
        const data   = Object.fromEntries(new FormData(this));

        const resp = await FrApi[method](url, data);

        FrApi.handleResponse(resp, () => {
            setTimeout(() => window.location.href = '@Url.Action("Index")', 800);
        });

        btn.disabled = false;
        btn.innerHTML = '@(esEdicion ? "Guardar cambios" : "Crear [Módulo]")';
    });
</script>
}
```

---

### Helper C# para Badges

```csharp
// Freiroute.Aplicacion/Helpers/FrHelper.cs
namespace Freiroute.Aplicacion.Helpers;

public static class FrHelper
{
    public static string BadgeClase(string? estado) => estado switch
    {
        "DRAFT"            => "fr-badge-neutral",
        "CONFIRMED"        => "fr-badge-info",
        "ASSIGNED"         => "fr-badge-info",
        "PICKUP_SCHEDULED" => "fr-badge-info",
        "IN_TRANSIT"       => "fr-badge-warning",
        "DELIVERED"        => "fr-badge-success",
        "INVOICED"         => "fr-badge-success",
        "CLOSED"           => "fr-badge-neutral",
        "CANCELLED"        => "fr-badge-danger",
        "ON_HOLD"          => "fr-badge-warning",
        "FAILED_DELIVERY"  => "fr-badge-danger",
        _                  => "fr-badge-neutral"
    };
}
```

---

### Checklist de Entregable Frontend

- [ ] `freiroute.css` actualizado con los estilos del módulo (si hay componentes nuevos)
- [ ] `_Layout.cshtml` con enlace del nuevo módulo en la sección correcta del sidebar
- [ ] **Index.cshtml**: tabla `fr-table`, badges semánticos, paginación `fr-pagination`, filtros, acciones por rol
- [ ] **Create.cshtml**: formulario con clases `fr-form-*`, validación jQuery Validate, envío AJAX con `FrApi`
- [ ] **Edit.cshtml**: misma estructura que Create, con datos prellenados
- [ ] Badges con `FrHelper.BadgeClase(estado)` — nunca colores hardcodeados
- [ ] IDs y códigos con clase `fr-id-code` (fuente JetBrains Mono)
- [ ] Toasts con `FrToast.success/error/warning` — nunca `alert()`
- [ ] Desactivación con `data-fr-deactivate` — nunca `confirm()` nativo
- [ ] Mensajes de validación en español consistentes con FluentValidation del servidor
- [ ] Acciones protegidas con `User.HasPermission("[modulo]", "ACTION")`
- [ ] `ViewData["ActiveMenu"]` asignado para resaltar el ítem correcto del sidebar
- [ ] Responsivo en 1280×720px mínimo
- [ ] Sin colores hardcodeados — solo variables CSS `var(--fr-*)`

---

## Contexto Freiroute TMS

El frontend debe reflejar la identidad de un TMS de nivel mundial: sidebar navy profesional con íconos Tabler, KPI cards en el dashboard, tablas densas con información de embarques y estados semánticos, mapas para track & trace, y formularios claros para registro de órdenes y carriers. El operador/dispatcher vive 8+ horas en esta interfaz — la velocidad, claridad y consistencia son prioritarias sobre la decoración.
