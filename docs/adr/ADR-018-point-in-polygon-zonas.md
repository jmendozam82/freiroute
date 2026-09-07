# ADR-018: Point-in-Polygon Manual para Zonas de Entrega

## Estado
Aceptado

## Fecha
2026 — Sprint 3

## Contexto
HU-016 requiere verificar si una ubicación con coordenadas cae dentro de una zona de entrega (CA-05). Las zonas tipo `POLIGONO` almacenan su área como GeoJSON (`poligono_geojson`) y el sistema debe resolver el problema clásico de geometría **point-in-polygon** (¿está el punto (lat, lng) dentro del polígono?).

Existen dos caminos: usar una librería de geometría espacial (NetTopologySuite, GeoAPI) o implementar el algoritmo manualmente. La decisión impacta dependencias del proyecto, complejidad de la BLL y precisión del resultado.

## Decisión
Implementar **point-in-polygon manual (ray casting / even-odd rule)** dentro de `ZonaEntregaService` en la BLL, sin librería externa de geometría.

1. **Ray casting:** se traza un rayo horizontal desde el punto y se cuentan las intersecciones con los lados del polígono. Número impar de cruces → el punto está dentro (teorema even-odd).
2. **Solo se aplica a zonas con `TipoDefinicion = POLIGONO` y `PoligonoGeoJson` no vacío.** Los demás métodos de definición (CODIGOS_POSTALES, CIUDADES, DEPARTAMENTOS, PAISES) se resuelven por comparación de listas en SQL (IZonaEntregaRepository.GetZonasPorPuntoAsync) contra el `empresa_id` — sin geometría.
3. **Soporta `Polygon` y `MultiPolygon`** del GeoJSON (primer anillo = anillo exterior; los `holes` (anillos interiores) se evalúan con la misma regla: dentro del exterior y fuera de todo hole).
4. **Se ignora el tercer componente** si viene (coordenadas [lng, lat] o [lng, lat, alt]); el GeoJSON usa el estándar RFC 7946: **longitud primero, latitud segundo** — la conversión se hace al parsear.
5. **Soporte de anillo en cruz antimeridiano:** no se soporta en esta versión. Las zonas del MVP están en Centroamérica (Nicaragua), lejos del antimeridiano. Documentado como limitación.

## Implementación

```csharp
// Ubicado en Freiroute.BLL/Services/ZonaEntregaService.cs (método privado)
// Algoritmo: ray casting (even-odd). Complejidad O(n) por polígono.
private static bool PuntoEnPoligono(double lat, double lng,
    IReadOnlyList<(double Lng, double Lat)> anillo)
{
    bool dentro = false;
    for (int i = 0, j = anillo.Count - 1; i < anillo.Count; j = i++)
    {
        var (lngI, latI) = anillo[i];
        var (lngJ, latJ) = anillo[j];
        if ((latI > lat) != (latJ > lat) &&
            lng < (lngJ - lngI) * (lat - latI) / (latJ - latI) + lngI)
        {
            dentro = !dentro;
        }
    }
    return dentro;
}
```

Flujo en `VerificarPertenenciaAsync`:
1. `GetZonasPorPuntoAsync(lat, lng, empresaId)` → zonas por listas (SQL) + candidatas POLIGONO.
2. Para cada candidata POLIGONO → parsear GeoJSON → evaluar con ray casting.
3. Retornar las zonas que coinciden (UNION de ambos resultados).

## Alternativas Consideradas

1. **NetTopologySuite (NTS)** — La librería estándar de geometría en .NET. Descartada para Sprint 3: agrega una dependencia pesada en la BLL para un único caso de uso. Se revisa en Sprint 9 (Route Optimization) cuando la geometría sea central y se pueda ubicar en un servicio dedicado `IGeometriaService`.
2. **PostGIS en PostgreSQL** — La opción más robusta (funciones `ST_Contains`, índices espaciales). Descartada para el MVP porque Supabase no la habilita en el tier gratuito y complicaría las migraciones. Candidata para v1.5 si el volumen de polígonos lo justifica.
3. **Librería ligera (GeoJSON.NET + Clipper)** — Evaluada; aún así agrega dependencias para un algoritmo de ~15 líneas.

## Consecuencias

**Positivas:**
- Cero dependencias nuevas en la BLL — el algoritmo es determinista y 100% testeable con xUnit
- Los tests pueden cubrir: punto dentro, punto fuera, punto en borde, polígono con hole, MultiPolygon
- Compatible con el plan futuro: migrar a PostGIS o NTS sin cambiar el contrato `VerificarPertenenciaAsync`

**Negativas / Trade-offs:**
- No se soporta el antimeridiano (limitación aceptable para el mercado objetivo)
- Rendimiento O(n) por polígono — irrelevante para decenas/centenas de zonas por tenant
- El parseo de GeoJSON es manual: requiere tests sólidos contra JSON malformado (se rechaza con error de validación)

## Módulos Afectados
- HU-016: `VerificarPertenenciaAsync` en IZonaEntregaService y `GetZonasPorPuntoAsync` en IZonaEntregaRepository
- HU-020: la zonificación de ubicaciones alimenta el cálculo de tarifas por zona
- Sprint 9 (Route Optimization): puede reemplazar la implementación manual sin cambiar contratos