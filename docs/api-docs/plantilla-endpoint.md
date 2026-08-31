# Plantilla: Documentación de Endpoint API

**Módulo:** `[NombreModulo]`  
**Endpoint:** `HTTP /api/v1/[recurso]`  
**Versión:** 1.0  
**Fecha:** `YYYY-MM-DD`

---

## 1. Descripción General

| Campo | Descripción |
|---|---|
| **Método HTTP** | `GET | POST | PUT | DELETE` |
| **Ruta** | `/api/v1/[recurso]` |
| **Resumen** | Descripción breve de qué hace este endpoint |
| **Objetivo** | ¿Qué problema resuelve o qué acción permite? |
| **Permisos requeridos** | `[modulo]:[read|create|update]` |

---

## 2. Parámetros de Entrada

### 2.1 Query Parameters (Parámetros de query)

| Parámetro | Tipo | Obligatorio | Descripción | Valores posibles | Default |
|---|---|---|---|---|---|
| `page` | `int` | No | Número de página | `1, 2, 3, ...` | `1` |
| `pageSize` | `int` | No | Registros por página | `10, 20, 50, 100` | `20` |
| `empresaId` | `Guid` | **SÍ** | ID del tenant/empresa | UUID | Inyectado por middleware |
| `sortBy` | `string` | No | Campo para ordenar | Nombre de columna | `fecha_creacion` |
| `sortDirection` | `string` | No | Dirección ordenamiento | `asc` / `desc` | `desc` |
| `search` | `string` | No | Búsqueda por texto libre | Texto | - |
| `fechaInicio` | `DateTime` | No | Fecha inicio filtro | `YYYY-MM-DD` | - |
| `fechaFin` | `DateTime` | No | Fecha fin filtro | `YYYY-MM-DD` | - |

### 2.2 Body Parameters (Para POST/PATH)

```csharp
// Ejemplo de DTO de entrada
public class SolicitudDto
{
    [Required(ErrorMessage = "Este campo es obligatorio")]
    [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
    public string Nombre { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Valor debe ser mayor a 0")]
    public decimal Precio { get; set; }
    
    public Guid? IdReferencia { get; set; }
    public string? Observaciones { get; set; }
}
```

---

## 3. Respuestas esperadas

### 3.1 Éxito (200 OK o 201 Created)

```json
{
  "success": true,
  "data": {
    "id": "d2b9c8e4-7f5a-4bcd-9a2e-1f6d8b9c0a1e",
    "nombre": "Ejemplo",
    "precio": 150.50,
    "activo": true,
    "fechaCreacion": "2024-01-15T10:30:00Z"
  },
  "errors": null,
  "message": "Operación completada exitosamente",
  "timestamp": "2024-01-15T10:30:00Z",
  "requestId": "c8e4d2b9-7f5a-4bcd-9a2e-1f6d8b9c0a1e"
}
```

### 3.2 Errores

| Código HTTP | Estructura `ApiResponse` | Descuándo |
|---|---|---|
| **400** | `ApiResponse<object>` | Datos de entrada inválidos (FluentValidation) |
| **401** | `ApiResponse<object>` | Token JWT inválido o expirado |
| **403** | `ApiResponse<object>` | Usuario sin permisos para esta operación |
| **404** | `ApiResponse<object>` | Registro no encontrado |
| **409** | `ApiResponse<object>` | Conflicto (ej: dato duplicado) |
| **500** | `ApiResponse<object>` | Error interno del servidor |

### 3.3 Mensajes de Error Comunes

```json
{
  "success": false,
  "data": null,
  "errors": {
    "Nombre": ["El nombre es obligatorio"],
    "Precio": ["El precio debe ser mayor a 0"]
  },
  "message": "Validación fallida",
  "timestamp": "2024-01-15T10:30:00Z",
  "requestId": "c8e4d2b9-7f5a-4bcd-9a2e-1f6d8b9c0a1e"
}
```

---

## 4. Código C# (Controller)

```csharp
/// <summary>
/// Descripción del endpoint para documentación Swagger
/// </summary>
/// <param name="solicitud">Datos necesarios para crear/actualizar el recurso</param>
/// <param name="empresaId">ID del tenant (inyectado automáticamente por el middleware)</param>
/// <returns>ApiResponse con el resultado de la operación</returns>
/// <response code="200">Operación exitosa</response>
/// <response code="201">Recurso creado exitosamente</response>
/// <response code="400">Datos de entrada inválidos</response>
/// <response code="401">No autenticado - Token inválido o expirado</response>
/// <response code="403">Sin permisos - El usuario no tiene el permiso requerido</response>
/// <response code="404">El recurso solicitado no fue encontrado</response>
[RequirePermission("[modulo]", "[tipo]")]  // Ej: "órdenes:create"
[HttpPost]
[ProducesResponseType(typeof(ApiResponse<ResultadoDto>), 200)]
[ProducesResponseType(typeof(ApiResponse<object>), 400)]
[ProducesResponseType(typeof(ApiResponse<object>), 401)]
[ProducesResponseType(typeof(ApiResponse<object>), 403)]
[ProducesResponseType(typeof(ApiResponse<object>), 404)]
public async Task<IActionResult> Crear(
    [FromBody] SolicitudDto solicitud,
    [FromQuery] Guid empresaId // Inyectado por middleware - NO quitar
)
{
    // 1. Validación automática por FluentValidation
    // 2. Lógica de negocio en Service (BLL)
    // 3. Mapeo a ResponseDto
    // 4. Retornar ApiResponse<ResultadoDto>
    
    return Ok(resultado);
}
```

---

## 5. Reglas de Negocio Asociadas

| Regla | Descripción |
|---|---|
| **RN-Dxx** | [Nombre de la regla de dominio] |
| **RN-Uxx** | [Nombre de la regla universal SaaS] |
| **Validación** | [Qué se valida en FluentValidation] |
| **Permisos** | `[modulo]:[tipo]` requerido para acceder |
| **Concurrencia** | [Cómo se maneja el caso de race condition] |

---

## 6. Tests Necesarios

### 6.1 Unit Tests (BLL)

- [ ] `Crear_CuandoSolicitudValida_RetornaSuccess`
- [ ] `Crear_CuandoNombreVacio_LanzaValidationException`
- [ ] `Crear_CuandoEmpresaInexistente_RetornaError`
- [ ] `Crear_CuandoYaExiste_RetornaErrorDuplicado`

### 6.2 Integration Tests (API)

- [ ] `Crear_CuandoAutenticadoYValido_Retorna200`
- [ ] `Crear_SinToken_Retorna401`
- [ ] `Crear_SinPermiso_Retorna403`
- [ ] `Crear_CuandoDtoInvalido_Retorna400`
- [ ] `Crear_CuandoHayErrorDeNegocio_Retorna400_conMensajes`

### 6.3 Datos de Prueba (Test Data)

```csharp
var solicitudValida = new SolicitudDto
{
    Nombre = "Test Nombre",
    Precio = 100.50m
};

var tokenValido = JwtTestHelper.GenerateTestToken(
    userId: Guid.NewGuid(),
    tenantId: Guid.NewGuid(),
    permisos: new[] { "[modulo]:[tipo]" }
);
```

---

## 7. ADRs (Decisiones Arquitectónicas)

Si este endpoint requiere una decisión arquitectónica, referenciar:

- `ADR-NNN-descripcion.md` en `docs/adr/`

---

## 8. Checklist Pre-Deploy

- [ ] Endpoint creado en Controller
- [ ] `/// <summary>` agregado para Swagger
- [ ] `[Authorize]` + `[RequirePermission]` aplicado
- [ ] Respuesta `ApiResponse<T>` en todos los casos
- [ ] Filtro por `empresa_id` verificado
- [ ] FluentValidation configurado para inputs
- [ ] Tests unitarios creados y pasando
- [ ] Tests de integración creados y pasando
- [ ] Documentación en `api-docs.md` actualizada
- [ ] Cobertura de tests: BLL ≥ 80%, API ≥ 60%
- [ ] Headers de seguridad presentes
- [ ] Rate limiting configurado (si aplica)

---
*Plantilla estandarizada para documentación de endpoints API Freiroute TMS*  
*Última actualización: 2026*