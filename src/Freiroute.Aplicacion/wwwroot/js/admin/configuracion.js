document.addEventListener('DOMContentLoaded', () => {
    window.configuracion.cargarDatos();

    // Actualizar hex al cambiar input color
    document.getElementById('cfg_colorPrimario').addEventListener('input', function(e) {
        document.getElementById('hexPrimario').innerText = e.target.value.toUpperCase();
    });
    document.getElementById('cfg_colorSecundario').addEventListener('input', function(e) {
        document.getElementById('hexSecundario').innerText = e.target.value.toUpperCase();
    });

    // Preview de imagen al seleccionarla
    document.getElementById('inputFileLogo').addEventListener('change', function(e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function(evt) {
                document.getElementById('logoPreviewActual').src = evt.target.result;
                document.getElementById('logoPreviewActual').style.display = 'block';
                document.getElementById('logoEmptyText').style.display = 'none';
            };
            reader.readAsDataURL(file);
        }
    });
});

window.configuracion = {
    datosGenerales: null,

    cargarDatos: async function() {
        try {
            // Cargar datos generales
            const respGen = await FrApi.get('/api/configuracion');
            if (respGen.success) {
                this.datosGenerales = respGen.data;
                this.poblarDatosGenerales(this.datosGenerales);
            } else {
                FrToast.error('Error al cargar datos generales');
            }

            // Cargar numeración
            const respNum = await FrApi.get('/api/configuracion/numeracion');
            if (respNum.success) {
                this.poblarNumeracion(respNum.data);
            } else {
                FrToast.error('Error al cargar numeración');
            }
        } catch (error) {
            FrToast.error('Error de red al cargar configuración');
        }
    },

    poblarDatosGenerales: function(data) {
        // Tab 1
        document.getElementById('cfg_nombre').value = data.nombre || '';
        document.getElementById('cfg_rucNit').value = data.rucNit || '';
        document.getElementById('cfg_direccion').value = data.direccion || '';
        document.getElementById('cfg_telefono').value = data.telefono || '';
        document.getElementById('cfg_industria').value = data.industria || '';
        document.getElementById('cfg_sitioWeb').value = data.sitioWeb || '';
        document.getElementById('cfg_moneda').value = data.moneda || 'USD';
        document.getElementById('cfg_zonaHoraria').value = data.zonaHoraria || 'America/Managua';
        document.getElementById('cfg_formatoFecha').value = data.formatoFecha || 'DD/MM/YYYY';
        document.getElementById('cfg_nombreRemitente').value = data.nombreRemitente || '';
        document.getElementById('cfg_emailRemitente').value = data.emailRemitente || '';

        // Tab 2 (Colores y Logo)
        if (data.colorPrimario) {
            document.getElementById('cfg_colorPrimario').value = data.colorPrimario;
            document.getElementById('hexPrimario').innerText = data.colorPrimario.toUpperCase();
        }
        if (data.colorSecundario) {
            document.getElementById('cfg_colorSecundario').value = data.colorSecundario;
            document.getElementById('hexSecundario').innerText = data.colorSecundario.toUpperCase();
        }
        
        if (data.logoUrl) {
            document.getElementById('logoPreviewActual').src = data.logoUrl;
            document.getElementById('logoPreviewActual').style.display = 'block';
            document.getElementById('logoEmptyText').style.display = 'none';
            document.getElementById('btnEliminarLogo').style.display = 'inline-block';
        }
    },

    poblarNumeracion: function(data) {
        document.getElementById('num_prefijoOrden').value = data.prefijoOrden || '';
        document.getElementById('num_secOrden').innerText = data.secuenciaOrden || '1';

        document.getElementById('num_prefijoEmbarque').value = data.prefijoEmbarque || '';
        document.getElementById('num_secEmbarque').innerText = data.secuenciaEmbarque || '1';

        document.getElementById('num_prefijoCartaPorte').value = data.prefijoCartaPorte || '';
        document.getElementById('num_secCartaPorte').innerText = data.secuenciaCartaPorte || '1';
    },

    guardarGeneral: async function(e) {
        e.preventDefault();
        const btn = document.getElementById('btnGuardarGeneral');
        
        // Mantener los colores actuales en la petición PUT
        const requestData = {
            nombre: document.getElementById('cfg_nombre').value,
            rucNit: document.getElementById('cfg_rucNit').value,
            direccion: document.getElementById('cfg_direccion').value,
            telefono: document.getElementById('cfg_telefono').value,
            industria: document.getElementById('cfg_industria').value,
            sitioWeb: document.getElementById('cfg_sitioWeb').value,
            moneda: document.getElementById('cfg_moneda').value,
            zonaHoraria: document.getElementById('cfg_zonaHoraria').value,
            formatoFecha: document.getElementById('cfg_formatoFecha').value,
            nombreRemitente: document.getElementById('cfg_nombreRemitente').value,
            emailRemitente: document.getElementById('cfg_emailRemitente').value,
            colorPrimario: document.getElementById('cfg_colorPrimario').value,
            colorSecundario: document.getElementById('cfg_colorSecundario').value
        };

        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader ti-spin me-1"></i> Guardando...';

        try {
            const resp = await FrApi.put('/api/configuracion', requestData);
            if (resp.success) {
                FrToast.success('Datos generales actualizados');
                // Actualizar nombre en el sidebar si cambió
                document.querySelector('.fr-logo-tag').innerText = `TMS · ${requestData.nombre}`;
            } else {
                FrToast.error(resp.message || 'Error al guardar');
            }
        } catch (error) {
            FrToast.error('Error de red al guardar datos');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i> Guardar Datos Generales';
        }
    },

    guardarColores: async function(e) {
        e.preventDefault();
        const btn = document.getElementById('btnGuardarColores');
        
        // El endpoint PUT /api/configuracion recibe todos los campos, así que debemos enviar los demás también
        const requestData = {
            nombre: document.getElementById('cfg_nombre').value,
            rucNit: document.getElementById('cfg_rucNit').value,
            direccion: document.getElementById('cfg_direccion').value,
            telefono: document.getElementById('cfg_telefono').value,
            industria: document.getElementById('cfg_industria').value,
            sitioWeb: document.getElementById('cfg_sitioWeb').value,
            moneda: document.getElementById('cfg_moneda').value,
            zonaHoraria: document.getElementById('cfg_zonaHoraria').value,
            formatoFecha: document.getElementById('cfg_formatoFecha').value,
            nombreRemitente: document.getElementById('cfg_nombreRemitente').value,
            emailRemitente: document.getElementById('cfg_emailRemitente').value,
            colorPrimario: document.getElementById('cfg_colorPrimario').value,
            colorSecundario: document.getElementById('cfg_colorSecundario').value
        };

        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader ti-spin me-1"></i> Guardando...';

        try {
            const resp = await FrApi.put('/api/configuracion', requestData);
            if (resp.success) {
                FrToast.success('Colores actualizados');
                // Aplicar variables CSS en vivo
                document.documentElement.style.setProperty('--fr-navy-primary', requestData.colorPrimario);
                document.documentElement.style.setProperty('--fr-action-blue', requestData.colorSecundario);
            } else {
                FrToast.error(resp.message || 'Error al guardar colores');
            }
        } catch (error) {
            FrToast.error('Error de red al guardar colores');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="ti ti-palette me-1"></i> Guardar Colores';
        }
    },

    subirLogo: async function(e) {
        e.preventDefault();
        const fileInput = document.getElementById('inputFileLogo');
        const file = fileInput.files[0];
        if (!file) return;

        const btn = document.getElementById('btnGuardarLogo');
        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader ti-spin me-1"></i> Subiendo...';

        const formData = new FormData();
        formData.append('archivo', file);

        try {
            const response = await fetch('/api/configuracion/logo', {
                method: 'POST',
                body: formData,
                headers: {
                    'Authorization': `Bearer ${window.FrAuth?.getToken() || ''}`
                }
            });
            const data = await response.json();
            
            if (response.ok && data.success) {
                FrToast.success('Logo actualizado exitosamente');
                document.getElementById('btnEliminarLogo').style.display = 'inline-block';
                // Actualizar logo en el sidebar
                let sidebarLogo = document.querySelector('.fr-sidebar-logo img');
                if (!sidebarLogo) {
                    const logoMark = document.querySelector('.fr-sidebar-logo .fr-logo-mark');
                    if (logoMark) {
                        const img = document.createElement('img');
                        img.className = 'fr-logo-img';
                        img.style = 'max-height:32px; max-width:32px; object-fit:contain; border-radius:4px;';
                        logoMark.parentNode.replaceChild(img, logoMark);
                        sidebarLogo = img;
                    }
                }
                if (sidebarLogo) {
                    sidebarLogo.src = data.data; // data.data tiene la URL
                }
                fileInput.value = '';
            } else {
                FrToast.error(data.message || 'Error al subir logo');
            }
        } catch (error) {
            FrToast.error('Error de red al subir logo');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="ti ti-upload me-1"></i> Subir Logo';
        }
    },

    eliminarLogo: async function() {
        if (!confirm('¿Seguro que deseas eliminar el logo de la empresa?')) return;
        
        const btn = document.getElementById('btnEliminarLogo');
        btn.disabled = true;

        try {
            const resp = await FrApi.delete('/api/configuracion/logo');
            if (resp.success) {
                FrToast.success('Logo eliminado');
                document.getElementById('logoPreviewActual').style.display = 'none';
                document.getElementById('logoPreviewActual').src = '';
                document.getElementById('logoEmptyText').style.display = 'inline-block';
                btn.style.display = 'none';

                // Restaurar icono FR en el sidebar
                const sidebarLogo = document.querySelector('.fr-sidebar-logo img');
                if (sidebarLogo) {
                    const div = document.createElement('div');
                    div.className = 'fr-logo-mark';
                    div.innerText = 'FR';
                    sidebarLogo.parentNode.replaceChild(div, sidebarLogo);
                }
            } else {
                FrToast.error(resp.message || 'Error al eliminar logo');
                btn.disabled = false;
            }
        } catch (error) {
            FrToast.error('Error de red al eliminar logo');
            btn.disabled = false;
        }
    },

    guardarNumeracion: async function(e) {
        e.preventDefault();
        const btn = document.getElementById('btnGuardarNumeracion');
        const requestData = {
            prefijoOrden: document.getElementById('num_prefijoOrden').value,
            prefijoEmbarque: document.getElementById('num_prefijoEmbarque').value,
            prefijoCartaPorte: document.getElementById('num_prefijoCartaPorte').value
        };

        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader ti-spin me-1"></i> Guardando...';

        try {
            const resp = await FrApi.put('/api/configuracion/numeracion', requestData);
            if (resp.success) {
                FrToast.success('Prefijos de numeración actualizados');
            } else {
                FrToast.error(resp.message || 'Error al actualizar numeración');
            }
        } catch (error) {
            FrToast.error('Error de red al actualizar numeración');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="ti ti-device-floppy me-1"></i> Guardar Numeración';
        }
    }
};
