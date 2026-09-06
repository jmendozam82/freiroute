# Especificación Técnica: HU-014 Configuración General del Tenant

## 1. Descripción General
**Módulo:** Administración (Tenant)
**Ruta MVC:** `/Admin/Configuracion`
**Endpoints API:** `/api/configuracion` y `/api/configuracion/numeracion`

El módulo permite al administrador del tenant configurar los parámetros operativos, identidad visual, numeración de documentos y remesas de email para su empresa.

## 2. Requerimientos Funcionales (Frontend)
El UI se dividirá en Tabs (Pestañas) para organizar mejor la información:

- **Tab 1: Datos Generales:** 
  - Nombre, RUC/NIT, Dirección, Teléfono, Industria, Sitio Web.
  - Opciones de moneda (USD, EUR, etc.), Zona Horaria y Formato de fecha.
- **Tab 2: Identidad Visual:**
  - Subida de Logo (File input con previsualización).
  - Selector de Color Primario y Secundario (Input type color).
- **Tab 3: Numeración (Opcional por ahora si los campos son pocos, pero recomendado):**
  - Prefijos para Embarques, Órdenes, Cartas de Porte.
- **Tab 4: Email y Notificaciones:**
  - Email remitente, Nombre del remitente.

## 3. Comportamiento UI
- **Carga inicial:** El script debe llamar a `GET /api/configuracion` y poblar todos los campos del formulario.
- **Guardar General:** El botón "Guardar Cambios" envía un `PUT /api/configuracion` con todos los datos menos el logo y numeración.
- **Subir Logo:** Al seleccionar una imagen, se muestra el preview. Al darle guardar logo, hace `POST /api/configuracion/logo` con FormData.
- **Numeración:** La sección de numeración tiene su propio endpoint `PUT /api/configuracion/numeracion` y se carga con `GET /api/configuracion/numeracion`.

## 4. Consideraciones de Diseño (Freiroute Design System)
- Uso de componentes de formulario estándar (Bootstrap 5 + estilos Freiroute).
- Las pestañas usarán los `nav-tabs` de Bootstrap.
- Las notificaciones de éxito/error usarán `FrToast`.
