# ADR-017: Importación CSV Masiva — Estrategia Fail-Soft con Errores Parciales

## Estado
Aceptado

## Fecha
2026 — Sprint 3

## Contexto
Los catálogos de maestros del Sprint 3 requieren importación masiva desde CSV:
- HU-015 CA-06: ubicaciones con plantilla descargable
- HU-017 CA-06: tipos de mercancía
- HU-019 CA-08: clientes con validación de RUC único por empresa

Los archivos CSV de clientes reales suelen contener filas con errores (RUC duplicado, email mal formado, campos requeridos vacíos). Existen dos estrategias posibles: abortar todo el archivo ante el primer error, o procesar las filas válidas y reportar las inválidas.

El spec define el contrato de retorno como "número de registros importados exitosamente", lo que implica tolerancia a errores parciales. Este ADR fija la estrategia y el comportamiento exacto para todos los módulos.

## Decisión
La importación CSV es **fail-soft con errores parciales**:

1. **Se procesa fila por fila.** Una fila inválida NO aborta el archivo.
2. **Cada fila se valida** con las mismas reglas que el CRUD normal (FluentValidation) más las reglas de unicidad (RUC por empresa, código por empresa).
3. **Al final se retorna** el número de registros importados exitosamente (`int`).
4. **Las filas rechazadas se registran** en el log estructurado (Serilog) con número de línea y motivo completo — nunca datos sensibles.
5. **La importación es transaccional por fila, no por archivo.** Una fila que se inserta, se inserta; si falla a mitad de la fila (ej: duplicado en el último instante), la fila se rechaza y se continúa.
6. **Nunca se lanza excepción al usuario por filas inválidas.** El endpoint retorna 200 con el conteo de éxitos.

### Detalles por módulo
- **Ubicaciones:** las filas válidas se insertan; la geocodificación en background aplica después (ADR-014) — no bloquea la importación.
- **Clientes:** se valida RUC/NIT único por empresa (HU-019 CA-08) usando `ExisteRucAsync` con excludeId = null.
- **Mercancías:** se validan las reglas HAZMAT (clase ONU 1-9, rango de temperatura si requiere_refrigeracion).

## Implementación

```csharp
// Contrato estándar en todos los servicios con importación CSV
Task<int> ImportarCsvAsync(Stream csv, Guid empresaId);

// Estructura interna (cada servicio lo implementa igual):
// 1. Leer CSV con encabezados esperados (plantilla del sistema)
// 2. Por cada fila: mapear → validar (FluentValidation + unicidad)
// 3. Fila válida → CreateAsync. Fila inválida → _logger.LogWarning
//    con línea y motivo → continuar
// 4. Retornar contador de éxitos
```

## Alternativas Consideradas

1. **Fail-fast (abortar ante el primer error)** — Descartada: el usuario tendría que corregir y re-subir el archivo completo repetidamente; pésima UX para archivos de cientos de filas.
2. **Transacción completa (todo o nada)** — Descartada: una sola fila mala invalidaría las 500 buenas; el usuario no sabría cuáles se importaron.
3. **Retorno de detalle completo (filas + errores en el response)** — Posible evolución en v1.5 (tipo de retorno `ImportResultDto` con lista de errores). El MVP retorna solo el conteo para simplificar el contrato; los errores quedan en el log para diagnóstico.

## Consecuencias

**Positivas:**
- UX aceptable con archivos reales de clientes (los "suelos" de datos tienen errores de formato)
- Misma estrategia en los 3 módulos del sprint — comportamiento consistente y testeable
- Los QA pueden testear el escenario "archivo con 3 filas válidas y 2 inválidas → retorna 3"

**Negativas / Trade-offs:**
- El usuario no ve el detalle de qué filas fallaron (diferido a v1.5 con ImportResultDto)
- Requiere que los logs incluyan número de línea para diagnóstico — menos visible que un reporte en pantalla

## Módulos Afectados
- HU-015: ImportarCsvAsync en IUbicacionService
- HU-017: ImportarCsvAsync en ITipoMercanciaService
- HU-019: ImportarCsvAsync en IClienteService
- Futuros: cualquier importación CSV de sprints siguientes (embarques, carriers) hereda esta estrategia