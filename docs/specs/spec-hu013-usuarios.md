# Especificación Técnica: HU-013 Gestión de Usuarios por Tenant

## 1. Descripción General
**Módulo:** Administración (Tenant)
**Ruta MVC:** `/Admin/Usuarios`
**Endpoints API:** `/api/usuarios`

El módulo de usuarios permite al administrador del tenant visualizar, invitar, editar, y desactivar usuarios de su empresa. Los roles (perfiles) son asignados durante la invitación o edición.

## 2. Requerimientos Funcionales (Frontend)
- **Listado (Grid):** Mostrar usuarios activos y desactivados de la empresa. Columnas: Nombre, Email, Perfil, Estado, Último Acceso, Acciones.
- **Invitación de Usuario:** Modal para invitar a un nuevo usuario (Email, Nombre, Perfil). Llama a `POST /api/usuarios/invitar`.
- **Edición de Usuario:** Modal para editar un usuario existente. Llama a `PUT /api/usuarios/{id}`.
- **Desactivar Usuario:** Acción en el listado para desactivar (soft-delete). Llama a `PATCH /api/usuarios/{id}/deactivate`.
- **Reactivar Usuario:** Acción para reactivar usuarios desactivados. Llama a `PATCH /api/usuarios/{id}/reactivate`.

## 3. Consideraciones de Diseño (Freiroute Design System)
- Uso de `badge-fr-success` para estado ACTIVE, `badge-fr-warning` para PENDING, y `badge-fr-danger` para SUSPENDED/INACTIVE.
- Uso de `fr-table` para el grid de usuarios.
- Llamadas al API mediante el módulo utilitario `FrApi` provisto en `freiroute.js`.

## 4. Estructura Técnica

### 4.1. MVC Controller (`UsuariosController.cs`)
- Deberá inyectar `IPerfilService` para enviar a la vista `ViewData["Perfiles"]` necesario para poblar los `<select>` de los modales.

### 4.2. Vista (`Index.cshtml`)
- HTML Table con `id="usuariosTable"`.
- Modal `modalInvitarUsuario` (Formulario con Email, NombreCompleto, PerfilId).
- Modal `modalEditarUsuario` (Formulario para editar datos básicos y perfil).

### 4.3. Script (`usuarios.js` o integrado en la vista)
- Funciones JS para consumir `/api/usuarios`:
  - `cargarUsuarios()`
  - `invitarUsuario(event)`
  - `editarUsuario(event)`
  - `cambiarEstado(id, nuevoEstado)`
