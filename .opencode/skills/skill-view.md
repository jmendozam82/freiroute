# Skill: @FrontendDev (Desarrollo Frontend freiroute TMS)

## Rol
**@FrontendDev** implementa vistas Razor MVC, el Design System Freiroute, validación cliente con jQuery Validate, y toda la capa de presentación usando Bootstrap 5.3. Asegura que el UI sea responsivo, accesible, consistente y refleje fielmente los estados operacionales del TMS. Actúa después de @BackendDev (API endpoints listos) y entrega a @PM para revisión final.

---

## Responsabilidades

### 1. Lectura Obligatoria al Inicio de Sesión
```
1. Leer AGENTS.md completo — sección Design System Freiroute
2. Leer spec.md del módulo (docs/specs/HU-XXX-nombre.md)
3. Verificar API endpoints documentados por @BackendDev
4. Revisar Design System completo (docs/framework/freiroute-design-system.md)
5. Confirmar colores, tipografía y componentes del Design System
```

### 2. Posición en el Flujo de HU
```
@PM planifica Sprint
    → @Arquitecto define Entity + DTOs + Interfaces + ADR
    → @IngenieroDatos crea migración SQL + RLS
    → @BackendDev implementa BLL Service + FluentValidator + API Controller
    → @QA ejecuta tests + valida cobertura
    → @FrontendDev ← CREA VISTAS RAZOR + DESIGN SYSTEM FREIRROUTE
    → @PM revisa checklist completo + aprueba PR
```

### 3. Estructura de Directorios MVC

**Ruta base:** `src/Freiroute.Aplicacion/Areas/[Area]/Views/[Modulo]/`

| Módulo | Área | Ruta | Descripción |
|---|---|---|---|
| Empresas | Admin | `Areas/Admin/Views/Empresas/` | Gestión de tenants SaaS |
| Usuarios | Admin | `Areas/Admin/Views/Usuarios/` | Usuarios por tenant |
| Clientes | Tenant | `Areas/Tenant/Views/Clientes/` | Master de clientes |
| Carriers | Tenant | `Areas/Tenant/Views/Carriers/` | Transportistas |
| Ordenes | Portal | `Areas/Portal/Views/Ordenes/` | Órdenes de transporte |
| Embarques | Portal | `Areas/Portal/Views/Embarques/` | Embarques activos |

**Vistas estándar por módulo:**
```
Areas/[Area]/Views/[Modulo]/
├── Index.cshtml          # Listado paginado con filtros
├── Create.cshtml         # Formulario creación
├── Edit.cshtml           # Formulario edición
├── Details.cshtml        # Vista detalle
└── _ModalDeactivate.cshtml  # Modal confirmación desactivar
```

### 4. Layout Base — Sidebar + Topbar

**`Areas/[Area]/Views/Shared/_Layout.cshtml`:**
```html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>@ViewData["Title"] - freiroute TMS</title>

    <!-- Google Fonts: Inter (UI) + DM Sans (Display) -->
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=DM+Sans:wght@400;500;700&display=swap" rel="stylesheet">
    
    <!-- Tabler Icons -->
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@latest/tabler-icons.min.css">
    
    <!-- Bootstrap 5.3 CSS -->
    <link href="~/lib/bootstrap/dist/css/bootstrap.min.css" rel="stylesheet" />
    <!-- Design System Freiroute -->
    <link href="~/css/freiroute.css" rel="stylesheet" />
</head>
<body class="fr-bg-surface">

    <!-- ═══ SIDEBAR ═══ -->
    <aside class="sidebar sidebar-expanded">
        <div class="sidebar-brand">
            <img src="~/assets/logo-freiroute.svg" alt="Freiroute Logo" height="32" />
            <span class="brand-text">freiroute</span>
        </div>

        <nav class="sidebar-nav">
            <!-- Dashboard siempre visible -->
            <a asp-action="Dashboard" asp-controller="Home" class="sb-item @(ViewContext.ActionDescriptor?.ActionName == "Dashboard" ? "active" : "")">
                <i class="ti ti-layout-dashboard"></i> Dashboard
            </a>

            @if (ViewData["EsSuperAdmin"] != null && ViewData["EsSuperAdmin"].ToString() == "true") {
                <!-- Módulos Super Admin -->
                <a asp-area="Admin" asp-action="Index" asp-controller="Empresas" class="sb-item">
                    <i class="ti ti-building-company"></i> Empresas
                </a>
                <a asp-area="Admin" asp-action="Index" asp-controller="Usuarios" class="sb-item">
                    <i class="ti ti-users"></i> Usuarios
                </a>
            } else {
                <!-- Módulos Tenant según permisos -->
                @if (User.HasPermission("clientes", "READ")) {
                    <a asp-action="Index" asp-controller="Clientes" class="sb-item @(ViewContext.Controller.ValueProvider.GetValue("controller")?.FirstValue == "Clientes" ? "active" : "")">
                        <i class="ti ti-users-group"></i> Clientes
                    </a>
                }
                @if (User.HasPermission("carriers", "READ")) {
                    <a asp-action="Index" asp-controller="Carriers" class="sb-item @(ViewContext.Controller.ValueProvider.GetValue("controller")?.FirstValue == "Carriers" ? "active" : "")">
                        <i class="ti ti-truck"></i> Carriers
                    </a>
                }
                @if (User.HasPermission("ordenes", "READ")) {
                    <a asp-action="Index" asp-controller="Ordenes" class="sb-item @(ViewContext.Controller.ValueProvider.GetValue("controller")?.FirstValue == "Ordenes" ? "active" : "")">
                        <i class="ti ti-file-text"></i> Órdenes
                    </a>
                }
                @if (User.HasPermission("embarques", "READ")) {
                    <a asp-action="Index" asp-controller="Embarques" class="sb-item @(ViewContext.Controller.ValueProvider.GetValue("controller")?.FirstValue == "Embarques" ? "active" : "")">
                        <i class="ti ti-route"></i> Embarques
                    </a>
                }
            }
        </nav>

        <div class="sidebar-footer">
            <div class="user-info">
                <div class="user-avatar">@User.GetInitials()</div>
                <div class="user-name">@User.GetDisplayName()</div>
                <small class="user-role">@User.GetPerfilLabel()</small>
            </div>
            <a asp-action="Logout" asp-controller="Account" class="btn btn-link text-decoration-none">
                <i class="ti ti-logout"></i>
            </a>
        </div>
    </aside>

    <!-- ═══ MAIN CONTENT ═══ -->
    <div class="main-content">
        <!-- Topbar -->
        <header class="topbar">
            <div class="topbar-breadcrumb">
                <nav aria-label="breadcrumb">
                    <ol class="breadcrumb mb-0">
                        <li class="breadcrumb-item"><a asp-action="Dashboard" asp-controller="Home">Inicio</a></li>
                        @RenderBreadcrumbItems()
                    </ol>
                </nav>
            </div>
            <div class="topbar-actions">
                <button class="btn btn-icon btn-light" title="Notificaciones">
                    <i class="ti ti-bell"></i>
                    <span class="badge-notification">3</span>
                </button>
            </div>
        </header>

        <!-- Flash Messages -->
        @if (TempData["SuccessMessage"] != null) {
            <div class="alert alert-success alert-dismissible fade show fr-alert-shadow" role="alert">
                <i class="ti ti-check-circle"></i>
                @TempData["SuccessMessage"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }
        @if (TempData["ErrorMessage"] != null) {
            <div class="alert alert-danger alert-dismissible fade show fr-alert-shadow" role="alert">
                <i class="ti ti-alert-circle"></i>
                @TempData["ErrorMessage"]
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        }

        <!-- Content Body -->
        <main class="content-body">
            @RenderBody()
        </main>
    </div>

    <!-- Toast Container -->
    <div id="toastContainer" class="toast-container position-fixed top-0 end-0 p-3"></div>

    <!-- Scripts -->
    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
    <script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/freiroute.js"></script>
</body>
</html>
```

### 5. Design System CSS — freiroute.css

**`wwwroot/css/freiroute.css`:**
```css
/* ═══════════════════════════════════════════════════════════ */
/* Design System Freiroute TMS                                */
/* Versión 2.0 · Paleta operacional                           */
/* ═══════════════════════════════════════════════════════════ */

:root {
    /* Identidad Freiroute */
    --fr-navy-primary:   #0B2545;
    --fr-navy-mid:       #1B4F8A;
    --fr-action-blue:    #1A73E8;
    --fr-cyan-accent:    #00D4FF;
    --fr-blue-tint:      #E3F0FF;

    /* Semántica operacional TMS */
    --fr-success:        #2E7D32;
    --fr-success-light:  #E6F4EA;
    --fr-warning:        #F57F17;
    --fr-warning-light:  #FFF8E1;
    --fr-danger:         #E53935;
    --fr-danger-light:   #FFEBEE;
    --fr-draft:          #64748B;
    --fr-draft-light:    #F1F5F9;
    --fr-onhold:         #C2410C;
    --fr-onhold-light:   #FFF7ED;
    --fr-info:           #1A73E8;
    --fr-info-light:     #E3F0FF;

    /* Neutrales */
    --fr-surface-bg:     #F8FAFC;
    --fr-surface-card:   #FFFFFF;
    --fr-text-primary:   #1E293B;
    --fr-text-secondary: #64748B;
    --fr-border:         #E2E8F0;
    --fr-sidebar-w:      240px;
}

/* ── Body & Background ─────────────────────────────────── */
.fr-bg-surface {
    background-color: var(--fr-surface-bg);
    font-family: 'Inter', sans-serif;
}

/* ── KPI Cards ─────────────────────────────────────────── */
.kpi-card {
    border-radius: 10px;
    border: 1px solid var(--fr-border);
    background: var(--fr-surface-card);
    padding: 20px;
    transition: box-shadow 0.2s;
}
.kpi-card:hover {
    box-shadow: 0 4px 12px rgba(0,0,0,0.06);
}
.kpi-label {
    font-size: 11px;
    font-weight: 500;
    color: var(--fr-text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.05em;
}
.kpi-value {
    font-size: 28px;
    font-weight: 700;
    color: var(--fr-navy-primary);
    line-height: 1.2;
}
.kpi-up { color: var(--fr-success); }
.kpi-down { color: var(--fr-danger); }

/* ── Table Fr ──────────────────────────────────────────── */
.table-fr thead th {
    font-size: 11px;
    font-weight: 600;
    color: var(--fr-text-secondary);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    border-bottom: 2px solid var(--fr-border) !important;
    padding: 12px 16px;
    background: var(--fr-surface-bg);
}
.table-fr tbody tr {
    border-bottom: 1px solid var(--fr-border);
    transition: background 0.15s;
}
.table-fr tbody tr:hover {
    background: var(--fr-blue-tint);
}

/* ── Badges Operacionales ──────────────────────────────── */
.badge-fr {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    padding: 3px 10px;
    border-radius: 6px;
    font-size: 11px;
    font-weight: 600;
    letter-spacing: 0.02em;
}
.badge-fr-success   { background: var(--fr-success-light); color: var(--fr-success); }
.badge-fr-info      { background: var(--fr-info-light);    color: var(--fr-info); }
.badge-fr-warning   { background: var(--fr-warning-light); color: var(--fr-warning); }
.badge-fr-danger    { background: var(--fr-danger-light);  color: var(--fr-danger); }
.badge-fr-neutral   { background: var(--fr-draft-light);   color: var(--fr-draft); }
.badge-fr-onhold    { background: var(--fr-onhold-light);  color: var(--fr-onhold); }

/* ── Botones Fr ────────────────────────────────────────── */
.btn-fr-primary {
    background: var(--fr-action-blue);
    border: none;
    color: white;
    font-weight: 500;
    border-radius: 8px;
    padding: 8px 20px;
}
.btn-fr-primary:hover {
    background: var(--fr-navy-mid);
    color: white;
}

/* ── Search Bar ────────────────────────────────────────── */
.fr-search-bar {
    position: relative;
    max-width: 360px;
}
.fr-search-bar .form-control {
    padding-right: 40px;
    border-radius: 8px;
    border: 1px solid var(--fr-border);
    font-size: 13px;
}
.fr-search-bar .form-control:focus {
    border-color: var(--fr-action-blue);
    box-shadow: 0 0 0 3px rgba(26,115,232,0.15);
}
.fr-search-bar i {
    position: absolute;
    right: 12px;
    top: 50%;
    transform: translateY(-50%);
    color: var(--fr-text-secondary);
}
```

### 6. Vistas Estándar por Módulo

#### 6.1 Index.cshtml — Listado Paginado con Filtros

```html
@model PaginatedList<EmbarqueResponseDto>
@{
    ViewData["Title"] = "Gestión de Embarques";
    Layout = "~/Areas/Portal/Views/Shared/_Layout.cshtml";
}

<!-- Page Header -->
<div class="d-flex justify-content-between align-items-center mb-4">
    <div>
        <h1 class="page-title">Embarques</h1>
        <p class="text-muted mb-0">Gestione y rastree todos los embarques activos</p>
    </div>
    @if (User.HasPermission("embarques", "CREATE")) {
        <a asp-action="Create" class="btn btn-fr-primary">
            <i class="ti ti-plus"></i> Nuevo Embarque
        </a>
    }
</div>

<!-- KPI Dashboard Row -->
<div class="row g-3 mb-4">
    <div class="col-md-3 col-sm-6">
        <div class="kpi-card">
            <div class="kpi-label">Embarques Hoy</div>
            <div class="kpi-value">@ViewData["EmbarquesHoy"]</div>
            <div class="kpi-delta kpi-up">↑ 12% vs ayer</div>
        </div>
    </div>
    <div class="col-md-3 col-sm-6">
        <div class="kpi-card">
            <div class="kpi-label">En Tránsito</div>
            <div class="kpi-value text-fr-warning">@ViewData["EnTransito"]</div>
        </div>
    </div>
    <div class="col-md-3 col-sm-6">
        <div class="kpi-card">
            <div class="kpi-label">Entregados Hoy</div>
            <div class="kpi-value text-fr-success">@ViewData["EntregadosHoy"]</div>
        </div>
    </div>
    <div class="col-md-3 col-sm-6">
        <div class="kpi-card">
            <div class="kpi-label">SLA Incumplido</div>
            <div class="kpi-value text-fr-danger">@ViewData["SlaIncumplido"]</div>
        </div>
    </div>
</div>

<!-- Filters Panel -->
<div class="card card-fr mb-4">
    <div class="card-header d-flex align-items-center justify-content-between py-3 px-4">
        <h6 class="mb-0 fw-semibold"><i class="ti ti-filter me-2"></i>Filtros</h6>
        <button class="btn btn-sm btn-link text-decoration-none" onclick="toggleFilters()">Ocultar</button>
    </div>
    <div class="card-body pt-0" id="filterPanel">
        <form asp-action="Index" method="get" class="row g-3">
            <div class="col-md-3">
                <label class="form-label">Buscar</label>
                <div class="fr-search-bar">
                    <input type="text" name="buscar" class="form-control" 
                           value="@ViewData["Buscar"]" placeholder="Número embarque, cliente...">
                    <i class="ti ti-search"></i>
                </div>
            </div>
            <div class="col-md-2">
                <label class="form-label">Estado</label>
                <select name="estado" class="form-select">
                    <option value="">Todos</option>
                    <option value="DRAFT" selected="@(ViewData["Estado"] as string == "DRAFT")">Borrador</option>
                    <option value="CONFIRMED" selected="@(ViewData["Estado"] as string == "CONFIRMED")">Confirmado</option>
                    <option value="ASSIGNED" selected="@(ViewData["Estado"] as string == "ASSIGNED")">Asignado</option>
                    <option value="IN_TRANSIT" selected="@(ViewData["Estado"] as string == "IN_TRANSIT")">En tránsito</option>
                    <option value="DELIVERED" selected="@(ViewData["Estado"] as string == "DELIVERED")">Entregado</option>
                    <option value="FAILED_DELIVERY" selected="@(ViewData["Estado"] as string == "FAILED_DELIVERY")">Fallido</option>
                </select>
            </div>
            <div class="col-md-2">
                <label class="form-label">Desde</label>
                <input type="date" name="fechaDesde" class="form-control" value="@ViewData["FechaDesde"]">
            </div>
            <div class="col-md-2">
                <label class="form-label">Hasta</label>
                <input type="date" name="fechaHasta" class="form-control" value="@ViewData["FechaHasta"]">
            </div>
            <div class="col-md-3 d-flex align-items-end gap-2">
                <button type="submit" class="btn btn-fr-primary">Filtrar</button>
                <a asp-action="Index" class="btn btn-outline-secondary">Limpiar</a>
            </div>
        </form>
    </div>
</div>

<!-- Data Table -->
<div class="card card-fr">
    <div class="table-responsive">
        <table class="table table-fr mb-0">
            <thead>
                <tr>
                    <th>Número</th>
                    <th>Cliente</th>
                    <th>Origen → Destino</th>
                    <th>Estado</th>
                    <th>ETA</th>
                    <th>Carrier</th>
                    <th>OTD</th>
                    <th class="text-end">Acciones</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model)
                {
                    <tr>
                        <td>
                            <span class="code-font fw-medium">@item.NumeroEmbarque</span>
                        </td>
                        <td>@item.ClienteNombre</td>
                        <td>
                            <div class="fw-medium small">@item.OrigenCiudad</div>
                            <i class="ti ti-arrow-down text-muted small"></i>
                            <div class="fw-medium small">@item.DestinoCiudad</div>
                        </td>
                        <td>
                            @switch (item.Estado)
                            {
                                case "DRAFT":
                                    <span class="badge-fr badge-fr-neutral"><i class="ti ti-circle-dot"></i> Borrador</span>
                                    break;
                                case "CONFIRMED":
                                    <span class="badge-fr badge-fr-info"><i class="ti ti-circle-check"></i> Confirmado</span>
                                    break;
                                case "ASSIGNED":
                                    <span class="badge-fr badge-fr-info"><i class="ti ti-truck"></i> Asignado</span>
                                    break;
                                case "IN_TRANSIT":
                                    <span class="badge-fr badge-fr-warning"><i class="ti ti-truck-loading"></i> En tránsito</span>
                                    break;
                                case "DELIVERED":
                                    <span class="badge-fr badge-fr-success"><i class="ti ti-package-check"></i> Entregado</span>
                                    break;
                                case "FAILED_DELIVERY":
                                    <span class="badge-fr badge-fr-danger"><i class="ti ti-circle-x"></i> Fallido</span>
                                    break;
                                case "ON_HOLD":
                                    <span class="badge-fr badge-fr-onhold"><i class="ti ti-clock-hour-4"></i> En espera</span>
                                    break;
                                default:
                                    <span class="badge-fr badge-fr-neutral">@item.EstadoLabel</span>
                                    break;
                            }
                        </td>
                        <td>
                            @if (item.Eta.HasValue)
                            {
                                <span class="small @(item.SlaEnRiesgo ? "text-fr-warning fw-medium" : "")">
                                    @item.Eta.Value.ToString("HH:mm")
                                </span>
                            }
                            else { — }
                        </td>
                        <td class="small">@item.CarrierNombre ?? "-"</td>
                        <td>
                            <span class="code-font @(item.OtdCumplido ? "text-fr-success" : "text-fr-danger")">
                                @(item.OtdCumplido ? "✓" : "✗")
                            </span>
                        </td>
                        <td class="text-end">
                            @if (User.HasPermission("embarques", "UPDATE")) {
                                <a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-light me-1" title="Editar">
                                    <i class="ti ti-edit"></i>
                                </a>
                                <a asp-action="Details" asp-route-id="@item.Id" class="btn btn-sm btn-light" title="Detalle">
                                    <i class="ti ti-eye"></i>
                                </a>
                            }
                            @if (User.HasPermission("embarques", "CREATE")) {
                                <a asp-action="PrintPOD" asp-route-id="@item.Id" class="btn btn-sm btn-light" title="Imprimir POD">
                                    <i class="ti ti-printer"></i>
                                </a>
                            }
                        </td>
                    </tr>
                }
                @if (!Model.Any()) {
                    <tr>
                        <td colspan="8" class="text-center py-5 text-muted">
                            <i class="ti ti-package-off fs-1 d-block mb-2"></i>
                            No se encontraron embarques con los filtros aplicados
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>

    <!-- Pagination — 20 registros/página (RNF-01.4) -->
    <div class="card-footer d-flex justify-content-between align-items-center py-3 px-4">
        <small class="text-muted">
            Mostrando @Model.Count de @Model.TotalItemCount registros
            (página @Model.PageIndex de @Model.PageCount)
        </small>
        <ul class="pagination pagination-sm mb-0">
            <li class="page-item @(Model.HasPreviousPage ? "" : "disabled")">
                <a class="page-link" asp-action="Index" asp-route-page="$(Model.PageIndex - 1)">Anterior</a>
            </li>
            @for (int i = Math.Max(1, Model.PageIndex - 2); i <= Math.Min(Model.PageCount, Model.PageIndex + 2); i++) {
                <li class="page-item @(i == Model.PageIndex ? "active" : "")">
                    <a class="page-link" asp-action="Index" asp-route-page="@(i)">@i</a>
                </li>
            }
            <li class="page-item @(Model.HasNextPage ? "" : "disabled")">
                <a class="page-link" asp-action="Index" asp-route-page="@(Model.PageIndex + 1)">Siguiente</a>
            </li>
        </ul>
    </div>
</div>
```

#### 6.2 Create.cshtml / Edit.cshtml — Formulario con Validación

```html
@model EmbarqueRequestDto
@{
    ViewData["Title"] = Model.Id == Guid.Empty ? "Nuevo Embarque" : "Editar Embarque";
    Layout = "~/Areas/Portal/Views/Shared/_Layout.cshtml";
}

<!-- Breadcrumb -->
<nav aria-label="breadcrumb" class="mb-3">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a asp-action="Index" asp-controller="Embarques">Embarques</a></li>
        <li class="breadcrumb-item active">@ViewData["Title"]</li>
    </ol>
</nav>

<div class="row">
    <div class="col-lg-8">
        <div class="card card-fr">
            <div class="card-header py-3 px-4 d-flex justify-content-between align-items-center">
                <h6 class="mb-0 fw-semibold">Información del Embarque</h6>
                <a asp-action="Index" class="btn btn-sm btn-light">
                    <i class="ti ti-arrow-left"></i> Volver
                </a>
            </div>
            <div class="card-body p-4">
                <form id="formEmbarque" asp-action="Create" asp-controller="Embarques" method="post">
                    @if (Model.Id != Guid.Empty) {
                        <input type="hidden" asp-for="Id" />
                    }

                    <input type="hidden" id="empresaId" name="EmpresaId" 
                           value="@User.GetEmpresaId()" />

                    <!-- Sección: Datos Generales -->
                    <h6 class="fw-semibold mb-3 pb-2 border-bottom">
                        <i class="ti ti-info-circle me-1"></i>Datos Generales
                    </h6>
                    <div class="row g-3 mb-4">
                        <div class="col-md-6">
                            <label asp-for="ClienteId" class="form-label">Cliente <span class="text-danger">*</span></label>
                            <select asp-for="ClienteId" class="form-select"
                                    asp-items="@(new SelectList((IEnumerable<SelectListItem>)ViewBag.Clientes, "Id", "Nombre"))"
                                    data-val="true"
                                    data-val-required="Debe seleccionar un cliente">
                                <option value="">-- Seleccionar Cliente --</option>
                            </select>
                            <span asp-validation-for="ClienteId" class="text-danger small"></span>
                        </div>
                        <div class="col-md-6">
                            <label asp-for="TipoCarga" class="form-label">Tipo de Carga</label>
                            <select asp-for="TipoCarga" class="form-select">
                                <option value="FTL">FTL — Camión Completo</option>
                                <option value="LTL">LTL — Carga Parcial</option>
                                <option value="EXCEPTIONAL">Excepcional</option>
                            </select>
                            <span asp-validation-for="TipoCarga" class="text-danger small"></span>
                        </div>
                    </div>

                    <!-- Sección: Origen y Destino -->
                    <h6 class="fw-semibold mb-3 pb-2 border-bottom">
                        <i class="ti ti-map-pin me-1"></i>Ruta
                    </h6>
                    <div class="row g-3 mb-4">
                        <div class="col-md-6">
                            <label asp-for="OrigenUbicacionId" class="form-label">Origen <span class="text-danger">*</span></label>
                            <select asp-for="OrigenUbicacionId" class="form-select"
                                    data-val="true" data-val-required="Seleccione origen">
                                <option value="">-- Seleccionar Origen --</option>
                            </select>
                            <span asp-validation-for="OrigenUbicacionId" class="text-danger small"></span>
                        </div>
                        <div class="col-md-6">
                            <label asp-for="DestinoUbicacionId" class="form-label">Destino <span class="text-danger">*</span></label>
                            <select asp-for="DestinoUbicacionId" class="form-select"
                                    data-val="true" data-val-required="Seleccione destino">
                                <option value="">-- Seleccionar Destino --</option>
                            </select>
                            <span asp-validation-for="DestinoUbicacionId" class="text-danger small"></span>
                        </div>
                    </div>

                    <!-- Sección: Fecha y Tiempo -->
                    <h6 class="fw-semibold mb-3 pb-2 border-bottom">
                        <i class="ti ti-calendar me-1"></i>Cronograma
                    </h6>
                    <div class="row g-3 mb-4">
                        <div class="col-md-4">
                            <label asp-for="FechaPickupPlanificada" class="form-label">Fecha Pickup</label>
                            <input asp-for="FechaPickupPlanificada" type="datetime-local" class="form-control" />
                            <span asp-validation-for="FechaPickupPlanificada" class="text-danger small"></span>
                        </div>
                        <div class="col-md-4">
                            <label asp-for="FechaEntregaRequerida" class="form-label">Entrega Requerida</label>
                            <input asp-for="FechaEntregaRequerida" type="datetime-local" class="form-control"
                                   data-val="true"
                                   data-val-required="Fecha requerida"
                                   data-val-greaterthan="Debe ser posterior al pickup" />
                            <span asp-validation-for="FechaEntregaRequerida" class="text-danger small"></span>
                        </div>
                    </div>

                    <!-- Sección: Detalles de Carga -->
                    <h6 class="fw-semibold mb-3 pb-2 border-bottom">
                        <i class="ti ti-package me-1"></i>Detalles de Carga
                    </h6>
                    <div class="row g-3 mb-4">
                        <div class="col-md-3">
                            <label asp-for="PesoTotal" class="form-label">Peso (kg)</label>
                            <input asp-for="PesoTotal" type="number" step="0.01" min="0" max="50000" class="form-control" />
                            <span asp-validation-for="PesoTotal" class="text-danger small"></span>
                        </div>
                        <div class="col-md-3">
                            <label asp-for="VolumenTotal" class="form-label">Volumen (m³)</label>
                            <input asp-for="VolumenTotal" type="number" step="0.01" min="0" class="form-control" />
                            <span asp-validation-for="VolumenTotal" class="text-danger small"></span>
                        </div>
                        <div class="col-md-3">
                            <label asp-for="CostoFlete" class="form-label">Costo Flete</label>
                            <input asp-for="CostoFlete" type="number" step="0.01" min="0" class="form-control" />
                            <span asp-validation-for="CostoFlete" class="text-danger small"></span>
                        </div>
                    </div>

                    <!-- Observaciones -->
                    <div class="mb-4">
                        <label asp-for="Observaciones" class="form-label">Observaciones</label>
                        <textarea asp-for="Observaciones" rows="3" class="form-control" maxlength="2000"></textarea>
                        <span asp-validation-for="Observaciones" class="text-danger small"></span>
                        <div class="form-text text-end">@Model.Observaciones.Length / 2000</div>
                    </div>

                    <!-- Acciones -->
                    <div class="d-flex gap-2 justify-content-end pt-3 border-top">
                        <a asp-action="Index" class="btn btn-outline-secondary">Cancelar</a>
                        <button type="submit" class="btn btn-fr-primary" id="btnSubmit">
                            <i class="ti ti-device-floppy me-1"></i>Guardar
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <!-- Info Panel Lateral -->
    <div class="col-lg-4">
        <div class="card card-fr mb-3">
            <div class="card-body">
                <h6 class="fw-semibold mb-3"><i class="ti ti-help-circle me-1"></i>Ayuda</h6>
                <ul class="list-unstyled mb-0">
                    <li class="mb-2 small">
                        <i class="ti ti-circle-filled text-fr-info me-2"></i>
                        El número de embarque se genera automáticamente
                    </li>
                    <li class="mb-2 small">
                        <i class="ti ti-circle-filled text-fr-warning me-2"></i>
                        Fecha de entrega debe ser posterior al pickup
                    </li>
                    <li class="small">
                        <i class="ti ti-circle-filled text-fr-danger me-2"></i>
                        Peso máximo por embarque: 50,000 kg
                    </li>
                </ul>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    @{await Html.RenderPartialAsync("_ValidationScriptsPartial");}
    <script src="~/js/modules/embarque-form.js"></script>
}
```

### 7. JavaScript — freiroute.js y Modulares

**`wwwroot/js/freiroute.js` — Utilidades globales:**
```javascript
/**
 * freiroute.js — Utilidades globales del Design System
 * Mantiene consistencia en toasts, confirmaciones y llamadas API
 */

// ── Toast Notification System ──────────────────────────────
function mostrarToast(tipo, titulo, mensaje, duracion = 5000) {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const bgClass = tipo === 'success' ? 'text-bg-success' :
                    tipo === 'error' ? 'text-bg-danger' :
                    tipo === 'warning' ? 'text-bg-warning' : 'text-bg-info';

    const icon = tipo === 'success' ? 'ti-check-circle' :
                 tipo === 'error' ? 'ti-alert-circle' :
                 tipo === 'warning' ? 'ti-alert-triangle' : 'ti-info-circle';

    const toastEl = document.createElement('div');
    toastEl.className = `toast align-items-center ${bgClass} border-0`;
    toastEl.setAttribute('role', 'alert');
    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <i class="ti ${icon} me-2"></i>
                <strong>${titulo}</strong> ${mensaje}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    container.appendChild(toastEl);
    const bsToast = new bootstrap.Toast(toastEl, { delay: duracion });
    bsToast.show();

    // Remover DOM cuando termine
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

// ── Confirm Modal Genérico ─────────────────────────────────
function confirmarAccion(titulo, mensaje, callback) {
    const modalDiv = document.createElement('div');
    modalDiv.className = 'modal fade';
    modalDiv.id = 'confirmModal';
    modalDiv.tabIndex = -1;
    modalDiv.innerHTML = `
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content" style="border-radius: 12px;">
                <div class="modal-header border-0 pb-0">
                    <h5 class="modal-title">${titulo}</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body pt-0">
                    <p>${mensaje}</p>
                </div>
                <div class="modal-footer border-0 pt-0">
                    <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Cancelar</button>
                    <button type="button" class="btn btn-danger" id="confirmBtn">Confirmar</button>
                </div>
            </div>
        </div>
    `;
    document.body.appendChild(modalDiv);
    const modal = new bootstrap.Modal(modalDiv);
    modal.show();

    document.getElementById('confirmBtn').onclick = () => {
        modal.hide();
        callback();
        modalDiv.remove();
    };
}

// ── API Call Helper ────────────────────────────────────────
async function apiCall(method, url, data = null) {
    const token = getJwtToken();
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        }
    };
    if (data) options.body = JSON.stringify(data);

    const response = await fetch(url, options);
    const result = await response.json();

    if (!response.ok) {
        if (result.errors) {
            result.errors.forEach(err => mostrarToast('error', 'Validación', err));
        } else {
            mostrarToast('error', 'Error', result.message || 'Error inesperado');
        }
        throw new Error(result.message);
    }

    mostrarToast('success', 'Éxito', result.message || 'Operación completada');
    return result;
}

function getJwtToken() {
    return localStorage.getItem('freiroute_token') || '';
}
```

**`wwwroot/js/modules/embarque-form.js` — Específico del módulo:**
```javascript
/**
 * empaque-form.js — Validación específica para formularios de embarque
 */
$(document).ready(function () {
    const form = $('#formEmbarque');

    // jQuery Validate + Unobtrusive ya configurado en _ValidationScriptsPartial
    $.validator.setDefaults({
        errorClass: 'is-invalid',
        validClass: 'is-valid',
        errorPlacement: function (error, element) {
            error.addClass('invalid-feedback');
            element.closest('.col-md-3, .col-md-4, .col-md-6').append(error);
        },
        highlight: function (element) {
            $(element).addClass('is-invalid').removeClass('is-valid');
        },
        unhighlight: function (element) {
            $(element).removeClass('is-invalid').addClass('is-valid');
        }
    });

    form.submit(function (e) {
        if ($(this).valid()) {
            // Deshabilitar botón para prevenir doble envío
            $('#btnSubmit').prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1"></span>Guardando...');

            $.ajax({
                url: form.attr('action'),
                type: form.attr('method') || 'POST',
                data: form.serialize(),
                success: function (response) {
                    mostrarToast('success', 'Registrado', response.message || 'Embarque guardado exitosamente');
                    setTimeout(() => window.location.href = '/embarques', 1000);
                },
                error: function (xhr) {
                    $('#btnSubmit').prop('disabled', false).html('<i class="ti ti-device-floppy me-1"></i>Guardar');
                    
                    if (xhr.responseJSON?.errors) {
                        xhr.responseJSON.errors.forEach(err => mostrarToast('error', 'Error', err));
                    } else {
                        mostrarToast('error', 'Error', 'No se pudo guardar el embarque');
                    }
                }
            });
            e.preventDefault();
        }
    });

    // Autocomplete de ubicaciones vía API
    $('#OrigenUbicacionId, #DestinoUbicacionId').on('change', function () {
        // Aquí se puede cargar coordenadas para mapa
        const val = $(this).val();
        if (val) loadLocationCoords(val);
    });
});

function loadLocationCoords(ubicacionId) {
    apiCall('GET', `/api/ubicaciones/${ubicacionId}/coords`)
        .then(coords => {
            console.log(`Coordenadas cargadas para ubicación ${ubicacionId}`);
            updateMapMarker(coords.lat, coords.lng);
        });
}
```

### 8. Componentes Operacionales TMS

#### Badges de Estado de Embarque (mapeo visual)

| Estado TMS | Clase Badge | Color Hex | Icono | Label |
|---|---|---|---|---|
| DRAFT | `badge-fr-neutral` | `#64748B` | `ti-circle-dot` | Borrador |
| CONFIRMED | `badge-fr-info` | `#1A73E8` | `ti-circle-check` | Confirmado |
| ASSIGNED | `badge-fr-info` | `#0891B2` | `ti-truck` | Asignado |
| IN_TRANSIT | `badge-fr-warning` | `#F57F17` | `ti-truck-loading` | En tránsito |
| DELIVERED | `badge-fr-success` | `#2E7D32` | `ti-package-check` | Entregado |
| FAILED_DELIVERY | `badge-fr-danger` | `#E53935` | `ti-circle-x` | Fallido |
| ON_HOLD | `badge-fr-onhold` | `#C2410C` | `ti-clock-hour-4` | En espera |
| CANCELLED | `badge-fr-neutral` | `#374151` | `ti-ban` | Cancelado |

#### KPI Cards Pattern

```html
<div class="kpi-card">
    <div class="kpi-label">[Título del KPI]</div>
    <div class="kpi-value [text-fr-success | text-fr-warning | text-fr-danger]">[Valor]</div>
    <div class="kpi-delta kpi-up">↑ 12% vs ayer</div>
</div>
```

### 9. Convenciones de Código Frontend

| Regla | Ejemplo Correcto | Prohibido |
|---|---|---|
| Razor helpers | `asp-action="Index"` | `<a href="/modulo/index">` manual |
| CSS classes | `form-label`, `form-control`, `form-select` | estilos inline |
| Iconos | `<i class="ti ti-truck"></i>` | emoji o imagen propia |
| Tooltips | `title="Descripción"` en botones | sin contexto |
| JS modular | `js/modules/[modulo]-form.js` | scripts sueltos en views |
| Validación HTML | `data-val-*` attributes | solo JS validation |
| Accesibilidad | `aria-label` en botones icon-only | sin labels |

### 10. Checklist de Entregable (revisado por @PM)

- [ ] Index.cshtml con listado paginado (20 items), filtros funcionales y tabla `table-fr`
- [ ] Create.cshtml con formulario completo, validación jQuery Validate (`data-val-*`)
- [ ] Edit.cshtml con valores prellenados desde controller
- [ ] Details.cshtml si aplica al módulo
- [ ] _Layout.cshtml con sidebar, breadcrumbs, flash messages y User context
- [ ] Diseño responsive: funciona en 1280×720px mínimo
- [ ] Colores y badges consistentes con Design System Freiroute (AGENTS.md UI/UX)
- [ ] Estados de embarque usan clases semánticas correctas (DRAFT→neutral, IN_TRANSIT→warning, etc.)
- [ ] Permisos UI: botones ocultos según rol del JWT
- [ ] Toasts de éxito/error funcionando con API integration
- [ ] Sin dependencias externas no permitidas (solo Bootstrap 5.3, jQuery, Tabler Icons)
- [ ] Textos y labels en español
- [ ] Sin warnings en build

### 11. Contexto Freiroute TMS — Diseño Orientado al Operador

@FrontendDev diseña interfaces para operadores logísticos que necesitan información rápida y clara:

**Principio UI/UX:** *"Menos es más — el dispatcher necesita tomar decisiones de transporte en segundos, no navegar por menús complejos."*

**Módulos MVP con requisitos UI específicos:**
- **Dashboard:** KPI cards con embarques hoy, OTD %, SLA en riesgo, alertas críticas
- **Embarques:** Tabla principal con status badges operacionales, ETA dinámico, acceso directo a POD
- **Órdenes:** Listado con filtros por estado DRAFT/CONFIRMED/CLOSED, botón de asignar carrier
- **Track & Trace:** Mapa integrado con markers GPS, timeline de eventos, geofences
- **Reportes:** Gráficos OTD, costos por carrier, tiempos de entrega, análisis de SLA
- **Master Data:** Formularios CRUD con validación fuerte y autocomplete inteligente

**Prioridades de diseño:**
1. **Performance visual:** tablas rápidas, carga progresiva de datos
2. **Scanability:** colores semánticos operacionales, badges claros, jerarquía tipográfica
3. **Eficiencia operativa:** atajos de teclado, acciones masivas, reusabilidad de formularios
4. **Mobile-first:** sidebar colapsable, tablas scroll-horiz en móvil, touch targets ≥ 44px

---

## Dependencias entre Agentes

| Recibe de | Entrega a | Formato de handoff |
|---|---|---|
| @BackendDev | API endpoints + Swagger docs | URLs `/api/[modulo]` documentadas |
| @QA | Tests aprobados (pasan + cobertura mínima) | PR aprobado por QA |
| @PM | Specs + criterios de aceptación | `docs/specs/HU-XXX-nombre.md` |
| @PM | Resultado de revisión visual/UI | Comments en PR con feedback |
