/* ================================================================
   FREIROUTE TMS — JavaScript Global v2.0
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

// ── Gestión de token JWT ───────────────────────────────────────
const FrAuth = {
    getToken() {
        return sessionStorage.getItem('fr_token') || '';
    },
    getTempToken() {
        return sessionStorage.getItem('fr_temp_token') || '';
    },
    setToken(token) {
        sessionStorage.setItem('fr_token', token);
    },
    clearToken() {
        sessionStorage.removeItem('fr_token');
        sessionStorage.removeItem('fr_temp_token');
        sessionStorage.removeItem('fr_tipo_usuario');
        sessionStorage.removeItem('fr_empresa_id');
    },
    isAuthenticated() {
        return !!this.getToken();
    }
};

// Alias global para compatibilidad con código existente
function getToken() { return FrAuth.getToken(); }

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
            while (container.children.length >= 3) {
                container.removeChild(container.firstChild);
            }
            container.appendChild(toast);
            setTimeout(() => {
                if (toast.parentNode) toast.remove();
            }, 4500);
        }
    },
    success: (msg, title) => FrToast.show(msg, 'success', title),
    error:   (msg, title) => FrToast.show(msg, 'error',   title),
    warning: (msg, title) => FrToast.show(msg, 'warning', title),
    info:    (msg, title) => FrToast.show(msg, 'info',    title)
};

// ── FrApi con Authorization header ─────────────────────────────
const FrApi = {
    _headers(extraHeaders = {}) {
        const token = FrAuth.getToken();
        return {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
            'RequestVerificationToken': document
                .querySelector('input[name="__RequestVerificationToken"]')
                ?.value || '',
            ...extraHeaders
        };
    },

    async get(url) {
        const resp = await fetch(url, {
            method: 'GET',
            headers: this._headers()
        });
        if (resp.status === 401) {
            FrAuth.clearToken();
            window.location.href = '/auth/login';
            return null;
        }
        return await resp.json();
    },

    async post(url, data) {
        const resp = await fetch(url, {
            method: 'POST',
            headers: this._headers(),
            body: JSON.stringify(data)
        });
        if (resp.status === 401) {
            FrAuth.clearToken();
            window.location.href = '/auth/login';
            return null;
        }
        if (resp.status === 202) {
            const body = await resp.json();
            return { status: 202, ...body };
        }
        return await resp.json();
    },

    async put(url, data) {
        const resp = await fetch(url, {
            method: 'PUT',
            headers: this._headers(),
            body: JSON.stringify(data)
        });
        if (resp.status === 401) {
            FrAuth.clearToken();
            window.location.href = '/auth/login';
            return null;
        }
        return await resp.json();
    },

    async delete(url) {
        const resp = await fetch(url, {
            method: 'DELETE',
            headers: this._headers()
        });
        if (resp.status === 401) {
            FrAuth.clearToken();
            window.location.href = '/auth/login';
            return null;
        }
        return await resp.json();
    },

    async patch(url) {
        const resp = await fetch(url, {
            method: 'PATCH',
            headers: this._headers()
        });
        if (resp.status === 401) {
            FrAuth.clearToken();
            window.location.href = '/auth/login';
            return null;
        }
        return await resp.json();
    },

    // Upload multipart (logo)
    async upload(url, formData) {
        const token = FrAuth.getToken();
        const resp = await fetch(url, {
            method: 'POST',
            headers: {
                ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
                'RequestVerificationToken': document
                    .querySelector('input[name="__RequestVerificationToken"]')
                    ?.value || ''
            },
            body: formData
        });
        return await resp.json();
    },

    handleResponse(response, onSuccess, onError) {
        if (!response) return;
        if (response.success) {
            if (onSuccess) onSuccess(response.data);
        } else {
            const msgs = response.errors?.length
                ? response.errors
                : [response.message || 'Error en la operación'];
            msgs.forEach(m => FrToast.error(m));
            if (onError) onError(response);
        }
    }
};

// ── Sistema de modales Bootstrap ───────────────────────────────
const FrModal = {
    show(id) {
        const el = document.getElementById(id);
        if (el) {
            const existing = bootstrap.Modal.getInstance(el);
            if (existing) existing.show();
            else new bootstrap.Modal(el).show();
        }
    },
    hide(id) {
        const el = document.getElementById(id);
        if (el) {
            bootstrap.Modal.getInstance(el)?.hide();
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
            'ACTIVE':           'fr-badge-success',
            'PENDING':          'fr-badge-info',
            'SUSPENDED':        'fr-badge-warning',
            'LOCKED':           'fr-badge-danger',
            'CANCELLED_EMPRESA':'fr-badge-danger'
        };
        return mapa[estado] || 'fr-badge-neutral';
    },
    claseActivo(activo) {
        return activo ? 'fr-badge-success' : 'fr-badge-neutral';
    }
};
