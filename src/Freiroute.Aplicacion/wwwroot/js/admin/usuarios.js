document.addEventListener('DOMContentLoaded', () => {
    window.usuarios.cargarUsuarios();
});

window.usuarios = {
    modalInvitar: null,
    modalEditar: null,
    listaUsuarios: [],
    listaFiltrada: [],
    currentPage: 1,
    pageSize: 5,

    initModals: function() {
        if (!this.modalInvitar) {
            this.modalInvitar = new bootstrap.Modal(document.getElementById('modalInvitar'));
        }
        if (!this.modalEditar) {
            this.modalEditar = new bootstrap.Modal(document.getElementById('modalEditar'));
        }
    },

    abrirModalInvitar: function() {
        this.initModals();
        document.getElementById('formInvitar').reset();
        this.modalInvitar.show();
    },

    abrirModalEditar: function(id) {
        this.initModals();
        const user = this.listaUsuarios.find(u => u.id === id);
        if (!user) return;

        document.getElementById('edit_id').value = user.id;
        document.getElementById('edit_email').value = user.email;
        document.getElementById('edit_nombreCompleto').value = user.nombreCompleto;
        document.getElementById('edit_telefono').value = user.telefono || '';
        document.getElementById('edit_perfilId').value = user.perfilId;
        
        this.modalEditar.show();
    },

    cargarUsuarios: async function() {
        try {
            const response = await FrApi.get('/api/usuarios');
            if (response.success) {
                this.listaUsuarios = response.data;
                this.listaFiltrada = [...this.listaUsuarios];
                this.filtrarUsuarios();
            } else {
                FrToast.error('Error al cargar usuarios: ' + response.message);
            }
        } catch (error) {
            FrToast.error('Error de red al cargar usuarios');
        }
    },

    renderTabla: function() {
        const tbody = document.querySelector('#usuariosTable tbody');
        tbody.innerHTML = '';

        const paginationEl = document.getElementById('usuariosPagination');

        if (this.listaFiltrada.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-muted">No hay usuarios registrados</td></tr>`;
            if (paginationEl) {
                paginationEl.classList.remove('d-flex');
                paginationEl.style.display = 'none';
            }
            return;
        }

        if (paginationEl) {
            paginationEl.style.display = '';
            paginationEl.classList.add('d-flex');
        }

        const totalItems = this.listaFiltrada.length;
        const totalPages = Math.ceil(totalItems / this.pageSize);
        if (this.currentPage > totalPages) this.currentPage = totalPages;
        if (this.currentPage < 1) this.currentPage = 1;

        const startIndex = (this.currentPage - 1) * this.pageSize;
        const endIndex = Math.min(startIndex + this.pageSize, totalItems);
        const pagedItems = this.listaFiltrada.slice(startIndex, endIndex);

        pagedItems.forEach(u => {
            const tr = document.createElement('tr');
            
            let estadoBadge = '';
            switch(u.estado) {
                case 'ACTIVE': estadoBadge = '<span class="badge-fr badge-fr-success">Activo</span>'; break;
                case 'PENDING': estadoBadge = '<span class="badge-fr badge-fr-warning">Pendiente</span>'; break;
                case 'SUSPENDED': estadoBadge = '<span class="badge-fr badge-fr-danger">Suspendido</span>'; break;
                default: estadoBadge = `<span class="badge-fr badge-fr-neutral">${u.estado}</span>`; break;
            }

            if (!u.activo) {
                estadoBadge = '<span class="badge-fr badge-fr-danger">Inactivo</span>';
            }

            const ultimoAcceso = u.ultimoAcceso 
                ? new Date(u.ultimoAcceso).toLocaleString() 
                : '<span class="text-muted">Nunca</span>';

            let acciones = '';
            if (u.activo) {
                acciones = `
                    <button class="btn btn-sm btn-light me-1" onclick="window.usuarios.abrirModalEditar('${u.id}')" title="Editar">
                        <i class="ti ti-pencil"></i>
                    </button>
                    <button class="btn btn-sm btn-danger text-white" onclick="window.usuarios.cambiarEstado('${u.id}', 'deactivate')" title="Desactivar">
                        <i class="ti ti-ban"></i>
                    </button>
                `;
            } else {
                acciones = `
                    <button class="btn btn-sm btn-success text-white" onclick="window.usuarios.cambiarEstado('${u.id}', 'reactivate')" title="Reactivar">
                        <i class="ti ti-check"></i>
                    </button>
                `;
            }

            tr.innerHTML = `
                <td>
                    <div class="d-flex align-items-center">
                        <div class="fr-avatar bg-light text-primary me-2 rounded-circle d-flex align-items-center justify-content-center" style="width: 32px; height: 32px; font-weight: 600;">
                            ${u.nombreCompleto.charAt(0).toUpperCase()}
                        </div>
                        <div>
                            <div class="fw-semibold text-dark">${u.nombreCompleto}</div>
                        </div>
                    </div>
                </td>
                <td class="text-muted">${u.email}</td>
                <td>${u.perfilNombre}</td>
                <td>${estadoBadge}</td>
                <td class="text-muted small">${ultimoAcceso}</td>
                <td class="text-end">${acciones}</td>
            `;
            tbody.appendChild(tr);
        });

        const infoEl = document.getElementById('paginationInfo');
        if (infoEl) infoEl.innerText = `Mostrando ${startIndex + 1}–${endIndex} de ${totalItems} registros`;
        
        const btnPrev = document.getElementById('btnPrevPage');
        if (btnPrev) btnPrev.disabled = this.currentPage === 1;
        
        const btnNext = document.getElementById('btnNextPage');
        if (btnNext) btnNext.disabled = this.currentPage === totalPages;
    },

    prevPage: function() {
        if (this.currentPage > 1) {
            this.currentPage--;
            this.renderTabla();
        }
    },

    nextPage: function() {
        const totalPages = Math.ceil(this.listaFiltrada.length / this.pageSize);
        if (this.currentPage < totalPages) {
            this.currentPage++;
            this.renderTabla();
        }
    },

    filtrarUsuarios: function() {
        const query = (document.getElementById('searchQuery')?.value || '').toLowerCase();
        const estado = document.getElementById('filterEstado')?.value;
        const perfilId = document.getElementById('filterPerfil')?.value;

        this.listaFiltrada = this.listaUsuarios.filter(u => {
            const matchesQuery = !query || 
                u.nombreCompleto.toLowerCase().includes(query) || 
                (u.email && u.email.toLowerCase().includes(query));
            
            let matchesEstado = true;
            if (estado === 'ACTIVE') matchesEstado = u.activo && u.estado === 'ACTIVE';
            else if (estado === 'INACTIVE') matchesEstado = !u.activo;
            else if (estado === 'PENDING') matchesEstado = u.activo && u.estado === 'PENDING';

            const matchesPerfil = !perfilId || u.perfilId === perfilId;

            return matchesQuery && matchesEstado && matchesPerfil;
        });

        this.currentPage = 1;
        this.renderTabla();
    },

    limpiarFiltros: function() {
        if (document.getElementById('searchQuery')) document.getElementById('searchQuery').value = '';
        if (document.getElementById('filterEstado')) document.getElementById('filterEstado').value = '';
        if (document.getElementById('filterPerfil')) document.getElementById('filterPerfil').value = '';
        this.filtrarUsuarios();
    },

    invitarUsuario: async function(e) {
        e.preventDefault();
        const btn = document.getElementById('btnSubmitInvitar');
        const form = e.target;
        const data = {
            email: form.email.value,
            nombreCompleto: form.nombreCompleto.value,
            perfilId: form.perfilId.value
        };

        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Enviando...';

        try {
            const response = await FrApi.post('/api/usuarios/invitar', data);
            if (response.success) {
                FrToast.success('Invitación enviada exitosamente');
                this.modalInvitar.hide();
                form.reset();
                this.cargarUsuarios();
            } else {
                FrToast.error(response.message || 'Error al invitar usuario');
            }
        } catch (error) {
            FrToast.error('Error de red al invitar usuario');
        } finally {
            btn.disabled = false;
            btn.innerHTML = 'Enviar Invitación';
        }
    },

    editarUsuario: async function(e) {
        e.preventDefault();
        const btn = document.getElementById('btnSubmitEditar');
        const form = e.target;
        const id = form.id.value;
        const data = {
            nombreCompleto: form.nombreCompleto.value,
            email: document.getElementById('edit_email').value,
            telefono: form.telefono.value,
            perfilId: form.perfilId.value
        };

        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Guardando...';

        try {
            const response = await FrApi.put(`/api/usuarios/${id}`, data);
            if (response.success) {
                FrToast.success('Usuario actualizado exitosamente');
                this.modalEditar.hide();
                this.cargarUsuarios();
            } else {
                FrToast.error(response.message || 'Error al actualizar usuario');
            }
        } catch (error) {
            FrToast.error('Error de red al actualizar usuario');
        } finally {
            btn.disabled = false;
            btn.innerHTML = 'Guardar Cambios';
        }
    },

    cambiarEstado: async function(id, accion) {
        const text = accion === 'deactivate' ? 'desactivar' : 'reactivar';
        if (!confirm(`¿Estás seguro que deseas ${text} este usuario?`)) return;

        try {
            const response = await FrApi.patch(`/api/usuarios/${id}/${accion}`);
            if (response.success) {
                FrToast.success(`Usuario ${text}do exitosamente`);
                this.cargarUsuarios();
            } else {
                FrToast.error(response.message || `Error al ${text} usuario`);
            }
        } catch (error) {
            FrToast.error(`Error de red al ${text} usuario`);
        }
    }
};
