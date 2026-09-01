# ADR-008: Patrón ApiResponse\<T\> en todas las respuestas REST

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
Las APIs REST del sistema Freiroute TMS devuelven datos a múltiples clientes (dashboard web MVC, portales de carriers, clientes enterprise vía EDI/API externa, futuras apps móviles). Se necesita un contrato de respuesta consistente que permita a los consumidores identificar inmediatamente si una operación fue exitosa o falló, con mensajes descriptivos y detalles opcionales para debugging. Las respuestas raw o inconsistentes generan errores de parsing en frontends y dificultan la integración con sistemas externos.

## Decisión
Todas las respuestas de la API USARÁN el wrapper `ApiResponse<T>`:

```csharp
namespace Freiroute.Utility.ApiResponse;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Factory methods estandarizados
    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Error(string message, List<string>? details = null) =>
        new() { Success = false, Message = message, Errors = details };
}
```

### Ejemplos de uso por escenario HTTP
```csharp
// ✅ Éxito 200 OK — consulta exitosa
return Ok(ApiResponse<List<EmpresaResponseDto>>.Ok(lista, "Consulta exitosa"));

// ✅ Éxito 201 Created — entidad recién creada
return CreatedAtAction(nameof(GetById), 
    new { id = resultado.Id }, 
    ApiResponse<EmpresaResponseDto>.Ok(resultado, "Entidad creada exitosamente"));

// ❌ Error 400 Bad Request — validación fallida
var validationErrors = ex.Errors.Select(e => e.ErrorMessage).ToList();
return BadRequest(ApiResponse<Unit>.Error("Validación fallida", validationErrors));

// ❌ Error 404 Not Found — registro no localizado
return NotFound(ApiResponse<Unit>.Error("El recurso solicitado no existe"));

// ❌ Error 500 Internal Server — falla inesperada
return StatusCode(500, ApiResponse<Unit>.Error(
    "Ocurrió un error interno al procesar su solicitud"));
```

Justificación principal: Unificar el formato de respuesta elimina ambigüedad entre servicios, simplifica consumo frontend (jQuery AJAX siempre espera `{ success, data, errors }`), y permite generar documentación Swagger automáticamente con schema único para toda la API.

## Alternativas Consideradas
1. **HTTP status codes tradicionales sin wrapper JSON** — Demasiada ambigüedad: un 200 OK con body vacío vs 404 NoContent se interpretan diferentemente según cliente. Sin estructura predictiva, cada consumidor reinventa parsing.
2. **RFC 7807 Problem Details (JSON)** — Estándar oficial pero muy orientado a errores. Nuestro dominio TMS necesita respuestas positivas igual de estructuradas. ApiResponse\<T\> es más general y compatible con ambos casos.
3. **gRPC protobuf schemas** — Sobredimensionado para MVP. No requiere serialización binaria ni protocolos de streaming. REST + JSON sigue siendo el estándar dominante para dashboards web corporativos.

## Consecuencias
**Positivas:**
- Formato uniforme identificable instantáneamente por cualquier cliente (frontend JS, Carrier Portal, integraciones externas)
- Documentación Swagger generada automáticamente desde tipos genéricos C#
- Frontend jQuery puede centralizar manejo de éxito/error en función reutilizable
- Testing unitario verifica siempre `.Success`, `.Data`, `.Errors` consistentemente

**Negativas / Trade-offs:**
- Padding adicional en payload JSON (~200 bytes extra por response normal)
- Consumidores legacy que esperen estructura nativa deben adaptarse
- Cada endpoint requiere envolver manualmente la respuesta (mitigable con Action Filter global)

## Módulos Afectados
Todos los controllers REST (`Freiroute.API/Controllers/`). Este ADR establece el contrato universal de comunicación API → cliente. Ningún endpoint público devolverá un tipo puro nunca. Solo `ApiResponse<T>`.

---
