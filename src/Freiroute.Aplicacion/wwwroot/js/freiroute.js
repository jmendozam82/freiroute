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

        const container = document.getElementById('frToastContainer');
        if (container) {
            container.appendChild(toast);
            setTimeout(() => toast.remove(), 4500);
        }
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
    async patch(url) {
        const resp = await fetch(url, {
            method: 'PATCH',
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
document.addEventListener('click', async e => {
    const btn = e.target.closest('[data-fr-deactivate]');
    if (!btn) return;
    e.preventDefault();
    const nombre = btn.dataset.frNombre || 'este registro';
    if (!confirm(`¿Está seguro de desactivar "${nombre}"? Esta acción puede revertirse.`)) return;
    const url = btn.dataset.frDeactivate;
    const resp = await FrApi.patch(url);
    FrApi.handleResponse(resp, () => {
        btn.closest('tr')?.remove();
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
            'FAILED_DELIVERY':  'fr-badge-danger',
            // Estados de empresa/tenant
            'ACTIVE':           'fr-badge-success',
            'SUSPENDED':        'fr-badge-warning',
            'CANCELLED_EMPRESA':'fr-badge-danger'
        };
        return mapa[estado] || 'fr-badge-neutral';
    },
    claseActivo(activo) {
        return activo ? 'fr-badge-success' : 'fr-badge-neutral';
    }
};
