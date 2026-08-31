# 📚 API Documentation — Freiroute TMS

**Sistema:** Freiroute TMS SaaS Multi-Tenant  
**Versión:** 1.0  
**Base URL:** `https://{tenant}.freiroute.com/api/v1`  
**Autenticación:** JWT Bearer Token  
**Formato Respuesta:** `ApiResponse<T>`  
**Documento generado:** Swagger/OpenAPI 3.0  

---

## 🔐 Autenticación

### Flujos de Autenticación Soportados

| Flujo | Endpoint | Descripción |
|---|---|---|
| **Login** | `POST /auth/login` | Autenticación con email y contraseña |
| **Google SSO** | `POST /auth/google` | Inicio sesión con cuenta Google |
| **Microsoft SSO** | `POST /auth/microsoft` | Inicio sesión con cuenta Microsoft |
| **Refresh Token** | `POST /auth/refresh` | Renovación de token JWT |
| **Recuperar Contraseña** | `POST /auth/recoverypassword` | Solicitud de restablecimiento por email |

### Headers Obligatorios

| Header | Valor | Descripción |
|---|---|---|
| `Authorization` | `Bearer {token}` | Token JWT válido |
| `x-tenant-id` | `UUID` | ID del tenant (contexto multi-tenant) |

---

## 📦 Estructura de Respuestas API

### Formato Estándar `ApiResponse<T>`

```json
{
  "success": true,
  "data": { /* objeto de respuesta */ },
  "errors": null,
  "message": "Operación completada exitosamente",
  "timestamp": "2024-01-15T10:30:00Z",
  "requestId": "unique-request-id"
}
```

### Errores Estándar

| Código | Estructura | Descripción |
|---|---|---|
| **400** | `ApiResponse<object>` | Solicitud inválida (validación FluentValidation) |
| **401** | `ApiResponse<object>` | Token inválido o expirado |
| **403** | `ApiResponse<object>` | Sin permisos para esta operación |
| **404** | `ApiResponse<object>` | Recurso no encontrado |
| **409** | `ApiResponse<object>` | Conflicto (ej: datos duplicados) |
| **500** | `ApiResponse<object>` | Error interno del servidor |

---

## 🗂️ Módulos y Endpoints

### Leyenda de Símbolos

| Símbolo | Significado |
|---|---|
| `GET` | Consultar/Listar datos |
| `POST` | Crear nuevo registro |
| `PUT` | Actualizar registro completo |
| `PATCH` | Actualizar parcialmente (si aplica) |
| `DELETE` | *No existe (soft delete)* |

### 📦 EP-01: Infraestructura y Autenticación

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/auth/login` | `POST` | Inicio sesión usuario | `auth:login` |
| `/auth/google` | `POST` | Login con Google | `auth:sso` |
| `/auth/microsoft` | `POST` | Login con Microsoft | `auth:sso` |
| `/auth/refresh` | `POST` | Renovación token | `auth:refresh` |
| `/users/perfiles` | `GET` | Listar perfiles disponibles | `perfiles:read` |
| `/users` | `GET` | Listar usuarios tenant | `usuarios:read` |

### 📦 EP-03: Gestión de Maestros (Catálogos)

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/catalogos/ubicaciones` | `GET` | Listar ubicaciones por tenant | `catálogos:read` |
| `/catalogos/ubicaciones` | `POST` | Crear nueva ubicación | `catálogos:create` |
| `/catalogos/ubicaciones/{id}` | `PUT` | Editar ubicación | `catálogos:update` |
| `/catalogos/ubicaciones/{id}` | `DELETE` (soft) | Desactivar ubicación | `catálogos:update` |
| `/catalogos/tipos-mercancía` | `GET` | Listar tipos de mercancía | `catálogos:read` |
| `/catalogos/zonas-entrega` | `GET` | Listar zonas de entrega | `catálogos:read` |
| `/catalogos/clientes` | `GET` | Listar clientes | `clientes:read` |
| `/catalogos/clientes` | `POST` | Crear cliente | `clientes:create` |
| `/catalogos/tiendas` | `GET` | Listar tarifas base | `tarifas:read` |

### 📦 EP-04: Order Management (Gestión de Órdenes)

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/órdenes` | `POST` | Crear orden manual | `órdenes:create` |
| `/órdenes/importar` | `POST` | Importar desde CSV/Excel | `órdenes:create` |
| `/órdenes/api` | `POST` | Recibir órdenes desde API/EDI | `órdenes:create` |
| `/órdenes/{id}` | `GET` | Obtener detalle orden | `órdenes:read` |
| `/órdenes/{id}/estados` | `PUT` | Cambiar estado orden | `órdenes:update` |
| `/órdenes/{id}/consolidar` | `POST` | Consolidar órdenes | `órdenes:update` |
| `/órdenes/{id}/dividir` | `POST` | Dividir orden | `órdenes:update` |

### 📦 EP-05: Carrier Management (Gestión de Transportistas)

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/transportistas` | `GET` | Listar transportistas | `transportistas:read` |
| `/transportistas` | `POST` | Registrar transportista | `transportistas:create` |
| `/transportistas/{id}` | `PUT` | Editar transportista | `transportistas:update` |
| `/transportistas/{id}/documentos` | `POST` | Subir documento | `transportistas:create` |
| `/transportistas/{id}/scorecard` | `GET` | Ver scorecard performance | `transportistas:read` |
| `/transportistas/{id}/contratos` | `GET` | Listar contratos | `transportistas:read` |
| `/transportistas/{id}/contratos` | `POST` | Registrar contrato | `transportistas:create` |

### 📦 EP-06: Shipment Planning (Planificación de Embarques)

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/embarques` | `POST` | Crear embarque | `embarques:create` |
| `/embarques` | `GET` | Listar embarques tenant | `embarques:read` |
| `/embarques/{id}` | `GET` | Detalle embarque | `embarques:read` |
| `/embarques/{id}` | `PUT` | Actualizar embarque | `embarques:update` |
| `/embarques/{id}/asignar-carrier` | `POST` | Asignar carrier | `embarques:update` |
| `/embarques/{id}/planificar-ruta` | `POST` | Planificación de ruta | `embarques:update` |

### 📦 EP-07: Route Optimization (Optimización de Rutas)

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/rutas/optimizar` | `POST` | Motor VRP optimización | `rutas:optimize` |
| `/rutas/visualizar` | `GET` | Visualizar rutas activas | `rutas:read` |
| `/rutas/ETA` | `GET` | Calcular ETA dinámico | `rutas:read` |
| `/rutas/geolocalizar` | `GET` | Geocodificación direcciones | `rutas:read` |

### 📦 EP-08: Track & Trace (Rastreo en Tiempo Real)

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/rastreo/posiciones` | `GET` | Posiciones vehículos tiempo real | `track:trace` |
| `/rastreo/eventos` | `GET` | Eventos trayecto | `track:trace` |
| `/rastreo/geofences` | `GET` | Geofences configurados | `track:trace` |
| `/rastreo/portal/{shipmentId}` | `GET` | Portal cliente rastreo | `track:trace` |

### 📦 EP-09: Document Management (Gestión Documental)

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/documentos/carta-porte` | `POST` | Generar Carta de Porte | `documentos:create` |
| `/documentos/manifiesto` | `POST` | Generar Manifiesto Carga | `documentos:create` |
| `/documentos/POD` | `POST` | Generar Proof of Delivery | `documentos:create` |
| `/documentos/{shipmentId}` | `GET` | Listar documentos embarque | `documentos:read` |
| `/documentos/{id}/POD` | `POST` | Firmar POD digital | `documentos:update` |

### 📦 EP-10: Freight Audit & Payment

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/audit/calcular-costo` | `POST` | Cálculo automático costo flete | `audit:calculate` |
| `/audit/factura` | `POST` | Cargar factura carrier | `audit:create` |
| `/pago/generar-factura` | `POST` | Generar factura cliente | `payment:create` |
| `/pago/cuentas-por-cobrar` | `GET` | Estado cuentas por cobrar | `payment:read` |

### 📦 EP-11: Customer Portal & CRM

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/clientes/portal/login` | `POST` | Login portal cliente | `cliente:login` |
| `/clientes/portal/dashboard` | `GET` | Dashboard cliente | `cliente:read` |
| `/clientes/portal/cotizar` | `POST` | Solicitar cotización | `cliente:create` |
| `/clientes/portal/notificaciones` | `GET` | Historial notificaciones | `cliente:read` |

### 📦 EP-12: Warehouse & Dock Management

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/muelles` | `GET` | Listar muelles | `warehouse:read` |
| `/muelles` | `POST` | Registrar muelle | `warehouse:create` |
| `/muelles/{id}` | `PUT` | Editar muelle | `warehouse:update` |
| `/patio/posiciones` | `GET` | Posiciones vehículos patio | `warehouse:read` |
| `/cross-docking/planificar` | `POST` | Planificar cross-docking | `warehouse:update` |

### 📦 EP-13: International & Customs

*(Por definir - módulos futuros)*

### 📦 EP-14: Fleet & Driver Management

*(Por definir - módulos futuros)*

### 📦 EP-15: Compliance & Safety

*(Por definir - módulos futuros)*

### 📦 EP-16: Analytics & Business Intelligence

*(Por definir - módulos futuros)*

### 📦 EP-17: Integraciones & API Pública

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/integraciones/webhook` | `POST` | Recibir webhook externos | `integraciones:create` |
| `/publico/health` | `GET` | Health check API | `publico:read` |
| `/publico/metrics` | `GET` | Métricas sistema | `publico:read` |

---

## 🔐 Permisos por Endpoint

### Estructura de Permisos

Cada endpoint debe tener asociado permisos en el formato:

```
`[modulo]:[tipo]`
```

Donde:
- `modulo`: Nombre del módulo (órdenes, transportistas, embarques, etc.)
- `tipo`: `read`, `create`, `update` (solo estos 3 permisos)

### Ejemplos

| Permiso | Significa |
|---|---|
| `órdenes:read` | Puede ver/listar órdenes |
| `órdenes:create` | Puede crear nuevas órdenes |
| `órdenes:update` | Puede editar/actualizar órdenes |
| `transportistas:read` | Puede ver datos de transportistas |
| `embarques:optimize` | Puede optimizar rutas (EP-07) |

### Verificación en Código

```csharp
[RequirePermission("órdenes", "read")]
[HttpGet]
public async Task<ApiResponse<List<OrdenResponseDto>>> GetAll()
{
    // Endpoint protegido - solo con permiso READ
}
```

---

## 📊 Parámetros de Paginación (Universal)

Todos los endpoints de listado (`GET`) soportan los siguientes parámetros de query:

| Parámetro | Tipo | Descripción | Valor por defecto |
|---|---|---|---|
| `page` | `int` | Número de página | `1` |
| `pageSize` | `int` | Registros por página | `20` |
| `maxPageSize` | `int` | Tamaño máximo permitido | `100` |
| `sortBy` | `string` | Campo para ordenar | `fecha_creacion` |
| `sortDirection` | `string` | Dirección (`asc`/`desc`) | `desc` |
| `empresaId` | `UUID` | Filtrar por tenant | Requerido (injectado por middleware) |

### Ejemplo

```
GET /api/v1/ordenes?page=2&pageSize=50&sortBy=fechaSolicitud&sortDirection=desc
```

---

## 📈 Rate Limiting (Por Tenant)

| Endpoint | Límite por ventana | Ventana |
|---|---|---|
| `/auth/login` | 5 intentos | 1 minuto |
| `/api/v1/*` | 100 requests | 1 minuto por tenant |
| `/swagger` | Ilimitado (solo desarrollo) | - |

---

## 🛡️ Headers de Seguridad

Todos los endpoints responden con estos headers de seguridad:

| Header | Valor |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `X-XSS-Protection` | `1; mode=block` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self' https://*; style-src 'self' 'unsafe-inline'` |

---

## 📝 Guía para Agentes: Cómo Documentar un Nuevo Endpoint

Cuando se esté creando un nuevo módulo o endpoint, seguir este formato:

### 1. Agregar el endpoint en el Controller

```csharp
/// <summary>
/// Descripción del endpoint para Swagger/OpenAPI
/// </summary>
/// <param name="dto">Datos de entrada</param>
/// <param name="empresaId">ID del tenant (inyectado por middleware)</param>
/// <returns>ApiResponse con el resultado</returns>
/// <response code="200">Éxito</response>
/// <response code="400">Error de validación</response>
/// <response code="401">No autenticado</response>
/// <response code="403">Sin permisos</response>
[RequirePermission("módulo", "create")]
[HttpPost]
public async Task<ApiResponse<ResultadoDto>> Crear(
    [FromBody] SolicitudDto dto,
    [FromQuery] Guid empresaId // Inyectado automáticamente por el middleware
)
{
    // Implementación
}
```

### 2. Agregar a la documentación en `api-docs.md`

Insertar en la tabla correspondiente del módulo:

| Endpoint | Método | Descripción | Permisos |
|---|---|---|---|
| `/ruta/nueva` | `POST` | Nueva funcionalidad | `módulo:create` |

### 3. Agregar tests de integración

Verificar en `API.Tests` que el endpoint:
- Retorna `200` o `201` con datos válidos
- Retorna `400` con datos inválidos
- Retorna `401` sin token
- Retorna `403` sin permisos

### 4. Agregar cobertura de tests

- Unit tests (BLL): ≥ 80% cobertura
- Integration tests (API): ≥ 60% cobertura

---

## 📥 Importar desde Swagger (Desarrollo)

En entorno desarrollo (`app.Environment.IsDevelopment()`):

1. Ejecutar la aplicación
2. Acceder a `/swagger` para ver documentación generada automáticamente
3. Usar `app.MapOpenApi()` para generar el archivo OpenAPI JSON/YAML
4. Copiar/pegar o sincronizar los endpoints principales a `api-docs.md`
5. Revisar y completar manualmente: descripciones, permisos, códigos de error

**Nota:** La documentación en `api-docs.md` es la fuente de verdad para producción, mientras que `/swagger` es para desarrollo y pruebas.

---

## 📋 Checklist de Validación por Módulo

Para cada módulo nuevo, verificar antes del deploy:

- [ ] Endpoints definidos en `api-docs.md`
- [ ] `/// <summary>` en cada acción del Controller
- [ ] `[Authorize]` + `[RequirePermission]` aplicado
- [ ] Respuesta `ApiResponse<T>` en todos los endpoints
- [ ] Parámetros de paginación en endpoints de listado
- [ ] Filtro por `empresa_id` (inyectado por middleware)
- [ ] Tests unitarios BLL creados (≥ 80% cobertura)
- [ ] Tests de integración API creados (≥ 60% cobertura)
- [ ] Swagger `/swagger` refleja la documentación
- [ ] Headers de seguridad presentes

---
*api-docs.md — Documentación de endpoints API Freiroute TMS*  
*Versión: 1.0.0 | Stack: ASP.NET Core 8 + Supabase + Dapper*  
*Última actualización: 2026*