# ADR-014: Geocodificación y Estrategia de Mapas

## Estado
Aceptado

## Fecha
2026 — Sprint 3

## Contexto
El TMS Freiroute requiere geocodificación de direcciones (convertir texto
a coordenadas lat/lng) para:
- Registrar ubicaciones de clientes, almacenes, puertos y aeropuertos
- Calcular distancias y tiempos de trayecto
- Visualizar rutas en el módulo Track & Trace (Sprint 10)
- Alimentar el motor de optimización de rutas (Sprint 9)

La decisión de proveedor impacta costo operativo, privacidad de datos
y disponibilidad offline. TMS de referencia (Oracle, SAP TM, Trimble)
usan HERE Maps o Google Maps como capa de geocodificación con OpenStreetMap
como fallback offline.

## Decisión
Freiroute usará una **estrategia dual**:

1. **Geocodificación server-side:** Nominatim (OpenStreetMap) vía API REST
   para geocodificar direcciones al registrar ubicaciones.
   Endpoint: `https://nominatim.openstreetmap.org/search`

2. **Mapas client-side:** Leaflet.js con tiles de CartoDB para visualización
   en las vistas de Track & Trace y planificación de rutas.

3. **Almacenamiento:** Las coordenadas (lat/lng) se persisten en BD como
   `DOUBLE PRECISION`. La dirección original se guarda siempre — las
   coordenadas son derivadas y pueden re-geocodificarse.

4. **Google Maps como alternativa futura:** La arquitectura permite
   cambiar el proveedor de geocodificación inyectando `IGeocodingService`
   distinto sin tocar la BLL ni las vistas.

## Implementación

### Interfaz de geocodificación
```csharp
// Freiroute.BLL/Interfaces/IGeocodingService.cs
public interface IGeocodingService
{
    Task<GeocodingResultDto?> GeocodeAsync(
        string direccion, string? ciudad = null, string? pais = null);

    Task<string?> ReverseGeocodeAsync(double lat, double lng);
}

// Freiroute.DTO/Geo/GeocodingResultDto.cs
public class GeocodingResultDto
{
    public double Latitud     { get; set; }
    public double Longitud    { get; set; }
    public string DireccionNormalizada { get; set; } = string.Empty;
    public double Confianza   { get; set; } // 0.0 - 1.0
    public string Proveedor   { get; set; } = "nominatim";
}
```

### Rate limiting Nominatim
Nominatim (OpenStreetMap) exige máximo 1 request/segundo y un
User-Agent identificado. La implementación debe:
- Incluir header `User-Agent: Freiroute-TMS/1.0 (contacto@freiroute.com)`
- Respetar 1 req/s con un semáforo o delay entre requests
- Cachear resultados de geocodificación en BD (no re-geocodificar
  la misma dirección normalizada)

### Tiles de mapa
```javascript
// CartoDB Positron (mapas en UI clara — formularios)
L.tileLayer(
  'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png',
  { attribution: '© OpenStreetMap © CartoDB' }
);

// CartoDB Dark Matter (Track & Trace — fondo oscuro)
L.tileLayer(
  'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
  { attribution: '© OpenStreetMap © CartoDB' }
);
```

## Alternativas Consideradas

1. **Google Maps Platform** — Descartada en fase inicial por costo
   ($5/1000 geocodificaciones). Revisable cuando el volumen de tenants
   justifique el gasto. La interfaz `IGeocodingService` permite migrar
   sin tocar la BLL.

2. **HERE Maps** — Usado por Oracle TMS y SAP TM. Mejor calidad en
   Latinoamérica que Nominatim. Descartada por costo ($0.50/1000 req
   en tier gratuito limitado). Candidato para plan ENTERPRISE en v2.0.

3. **Coordenadas manuales solamente** — Descartada porque degrada
   la experiencia del usuario al registrar ubicaciones. La geocodificación
   automática es estándar en TMS de clase mundial.

4. **MapBox** — Calidad similar a HERE, tier gratuito de 100,000 req/mes.
   Candidato secundario si Nominatim no cubre bien la región.

## Consecuencias

**Positivas:**
- Nominatim es gratuito y open-source — sin costo por request
- CartoDB tiles gratuitos sin API key para uso moderado
- La interfaz `IGeocodingService` permite cambiar proveedor sin
  impacto en BLL ni vistas
- Coordenadas almacenadas en BD — disponibles offline

**Negativas / Trade-offs:**
- Nominatim: calidad variable en direcciones rurales de Centroamérica
- Rate limit de 1 req/s — importaciones masivas de ubicaciones
  requieren throttling
- Sin autocompletado de direcciones en tiempo real
  (requiere Google Maps Places API — diferido a v2.0)

## Módulos Afectados
- HU-015: Gestión de ubicaciones y geocodificación (Sprint 3)
- HU-016: Zonas de entrega con polígonos (Sprint 3)
- EP-07: Route Optimization — consume coordenadas (Sprint 9)
- EP-08: Track & Trace — mapas en tiempo real (Sprint 10)
