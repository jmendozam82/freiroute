# Spec: Sprint 6 — EP-05 Shipments (Embarques) + Fixes Sprint 5

**Sprint:** 06  
**Épica:** EP-05 — Shipments (Embarques y Asignación de Transportistas)  
**Historias:** HU-033 · HU-034 · HU-035 · HU-036 · HU-037  
**Story Points:** 58 pts (HUs nuevas) + fixes deuda técnica (sin pts de backlog)  
**Objetivo del Sprint:** Resolver G-13 (bloqueo FK shipments), implementar el módulo
completo de embarques (CRUD, asignación carrier/conductor, consolidación real, track & trace
básico y documentos de embarque).  
**ADRs aplicables:** ADR-003 · ADR-005 · ADR-006 · ADR-007 · ADR-008 · ADR-012 ·
ADR-013 · ADR-014 · ADR-019 · ADR-020

---

## Dependencias

```
Sprint 1 → Auth, tenants, perfiles, permisos, auditoría                          ✅
Sprint 2 → Configuración del tenant, carriers, conductores                        ✅
Sprint 3 → Vehículos, ubicaciones, tarifas                                        ✅
Sprint 4 → ordenes (FSM, historial, numeración), shipments tabla esqueleto         ✅
Sprint 5 → Órdenes avanzadas (PO, prioridad, rechazos, SLA, reclamos)            ✅

Deuda técnica prioritaria a resolver ANTES de HUs nuevas:
  G-13 🔴 → @BackendDev: ConsolidarAsync FK 500 (inserta shipment ANTES de UPDATE ordenes)
  H-03 🟠 → @BackendDev: ReclamoValidator — agregar Must(BeValidUrl) para referencias_evidencia
  G-18 🟡 → @IngenieroDatos: JOIN listado paginado sin guard AND c.empresa_id = o.empresa_id
```

---

## Orden de Implementación por Capa

```
Fase 1 → @Arquitecto
         Constantes Utility: EstadoEmbarque, TipoDocumentoEmbarque, TipoEventoTrack
         Entities: Shipment (expand), EventoTrackTrace, DocumentoEmbarque
         DTOs: request/response para HU-033→HU-037
         Interfaces DAL: IShipmentRepository, IEventoTrackTraceRepository,
                         IDocumentoEmbarqueRepository
         Interfaces BLL: IShipmentService, ITrackTraceService, IDocumentoEmbarqueService
         Extends: ShipmentResponseDto con campos carrier, conductor, vehículo
         Reporta: "dotnet build: 0 errores ✔ — Listo para Fase 2"

Fase 2 → @IngenieroDatos
         Fix G-18: agregar guard AND c.empresa_id = o.empresa_id en OrdenRepository
         ALTER TABLE shipments → columnas completas (ver HU-033 SQL)
         CREATE TABLE eventos_track_trace
         CREATE TABLE documentos_embarque
         Índices + RLS en tablas nuevas
         Repositorios Dapper: ShipmentRepository (expand), EventoTrackTraceRepository,
                              DocumentoEmbarqueRepository
         Registro en DI
         Reporta: "supabase db diff vacío · dotnet build 0 errores ✔ — Listo para Fase 3"

Fase 3 → @BackendDev
         Fix G-13 PRIMERO: ShipmentService.ConsolidarAsync — INSERT shipment antes UPDATE ordenes
         Fix H-03: ReclamoValidator — validación URL en referencias_evidencia
         BLL: ShipmentService (HU-033 + HU-034 + HU-035),
              TrackTraceService (HU-036),
              DocumentoEmbarqueService (HU-037)
         Controllers: ShipmentsController (expand), TrackTraceController (nuevo),
                      DocumentosEmbarqueController (nuevo)
         Registro en DI
         Reporta: "dotnet build: 0 errores · dotnet test: N/N ✔ — Listo para Fase 4"

Fase 4 → @QA
         Tests BLL ≥80% cobertura acumulada
         Tests API ≥60% cobertura acumulada
         QA Report: docs/specs/sprint-06-qa-report.md
         Reporta: "N tests · BLL X% · API Y% · 0 fallos ✔ — Listo para Fase 5"

Fase 5 → @FrontendDev
         Vistas: Embarques (Index, Crear, Detalle, Asignar carrier/conductor),
                 Track & Trace (mapa Leaflet, timeline de eventos),
                 Documentos de embarque (listado, subir, preview signed URL)
         MVC Controllers en Areas/Tenant/Embarques
         Reporta: "dotnet build: 0 errores · smoke tests ✔ — Listo para @PM"
```

---

## Fixes de Deuda Técnica (sin HU, sin Story Points)

### Fix G-13 🔴 — ConsolidarAsync FK 500
**Agente:** @BackendDev · **Fase:** 3 (PRIMERO antes de cualquier servicio EP-05)  
**Archivo:** `src/BLL/Services/ShipmentService.cs` → método `ConsolidarAsync`  
**Causa raíz:** UPDATE ordenes.shipment_id se ejecuta antes del INSERT shipments → FK 23503.

```csharp
// Orden de operaciones correcta:
// 1. Validar ordenes (≥2, todas CONFIRMED, misma empresa)
// 2. INSERT INTO shipments (empresa_id, estado=PLANNED, ...) → shipmentId
// 3. UPDATE ordenes SET shipment_id = @ShipmentId, estado = 'ASSIGNED' WHERE id IN (...)
// 4. Registrar historial por cada orden
// 5. Registrar auditoría
```

**Tests a agregar:**
- `ConsolidarAsync_InsertaShipmentAntesDeActualizarOrdenes` (BLL)  
- `POST_Consolidar_RetornaShipmentIdReal_201` (API)

---

### Fix H-03 🟠 — ReclamoValidator sin validación URL
**Agente:** @BackendDev · **Fase:** 3  
**Archivo:** `src/BLL/Validators/ReclamoValidator.cs`  
**Fix:**

```csharp
// Agregar en el constructor del validator:
RuleForEach(x => x.ReferenciasEvidencia)
    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                 && (u.Scheme == "https" || u.Scheme == "http"))
    .WithMessage("Cada URL de evidencia debe ser una URL absoluta válida (http/https).")
    .When(x => x.ReferenciasEvidencia != null && x.ReferenciasEvidencia.Any());
```

**Test a agregar:** `ReclamoValidator_UrlInvalida_RetornaError`

---

### Fix G-18 🟡 — JOIN listado paginado sin guard empresa_id
**Agente:** @IngenieroDatos · **Fase:** 2  
**Archivo:** `src/DAL/Repositories/OrdenRepository.cs` · método `GetAllAsync`  
**Fix:** Agregar `AND c.empresa_id = o.empresa_id` en JOIN de clientes/ubicaciones del query paginado.  
**Test:** `GetAllAsync_JoinConGuardEmpresaId_NoLeaksCrossTeant`

---

## HU-033 · Gestión de embarques (CRUD)

**Como** dispatcher, **quiero** crear, consultar y gestionar embarques,  
**para** organizar el transporte de las órdenes confirmadas.

**Módulo de permiso:** `embarques`  
**Estimación:** 10 pts | **Prioridad:** Alta

### Máquina de estados del embarque

```
PLANNED → IN_TRANSIT → DELIVERED
    ↓           ↓
CANCELLED   ON_HOLD → IN_TRANSIT
```

| Estado      | Descripción                                         |
|-------------|-----------------------------------------------------|
| PLANNED     | Creado, pendiente de asignar carrier/conductor      |
| IN_TRANSIT  | En camino, conductor confirmó salida                |
| DELIVERED   | Entregado — estado terminal                         |
| CANCELLED   | Cancelado — estado terminal                         |
| ON_HOLD     | En espera (incidencia, retención aduanera, etc.)    |

### SQL — ALTER TABLE shipments

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_alter_shipments_ep05.sql
-- Expansión de la tabla shipments del esqueleto Sprint 4 (6 cols → completa)

ALTER TABLE shipments
    ADD COLUMN IF NOT EXISTS numero_shipment   VARCHAR(30),
    ADD COLUMN IF NOT EXISTS carrier_id        UUID REFERENCES carriers(id),
    ADD COLUMN IF NOT EXISTS conductor_id      UUID REFERENCES conductores(id),
    ADD COLUMN IF NOT EXISTS vehiculo_id       UUID REFERENCES vehiculos(id),
    ADD COLUMN IF NOT EXISTS fecha_programada  TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS fecha_salida_real TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS fecha_entrega_est TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS fecha_entrega_real TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS origen_id         UUID REFERENCES ubicaciones(id),
    ADD COLUMN IF NOT EXISTS destino_id        UUID REFERENCES ubicaciones(id),
    ADD COLUMN IF NOT EXISTS modo_transporte   VARCHAR(30) NOT NULL DEFAULT 'TERRESTRE',
    ADD COLUMN IF NOT EXISTS peso_total_kg     NUMERIC(12,2),
    ADD COLUMN IF NOT EXISTS volumen_total_m3  NUMERIC(12,3),
    ADD COLUMN IF NOT EXISTS notas             TEXT,
    ADD COLUMN IF NOT EXISTS creado_por        UUID REFERENCES usuarios(id);

-- Constraint de estado
ALTER TABLE shipments
    ADD CONSTRAINT ck_shipments_estado
        CHECK (estado IN ('PLANNED','IN_TRANSIT','DELIVERED','CANCELLED','ON_HOLD'));

-- Constraint de modo transporte
ALTER TABLE shipments
    ADD CONSTRAINT ck_shipments_modo_transporte
        CHECK (modo_transporte IN ('TERRESTRE','AEREO','MARITIMO','FERROVIARIO','INTERMODAL'));

CREATE INDEX IF NOT EXISTS idx_shipments_carrier_id   ON shipments(carrier_id);
CREATE INDEX IF NOT EXISTS idx_shipments_conductor_id ON shipments(conductor_id);
CREATE INDEX IF NOT EXISTS idx_shipments_estado       ON shipments(estado);
CREATE INDEX IF NOT EXISTS idx_shipments_fecha_prog   ON shipments(fecha_programada);

COMMENT ON TABLE shipments IS
    'Embarques de transporte. Expandido en Sprint 6 (EP-05) desde esqueleto Sprint 4.';
```

### Criterios de Aceptación

| CA | Descripción | Módulo permiso |
|----|-------------|----------------|
| CA-01 | `POST /api/embarques` → 201 con `numero_shipment` autogenerado (patrón ADR-020) | embarques:create |
| CA-02 | `GET /api/embarques` → listado paginado con filtros: estado, carrier_id, fecha_desde/hasta | embarques:read |
| CA-03 | `GET /api/embarques/{id}` → detalle completo con órdenes consolidadas, carrier, conductor | embarques:read |
| CA-04 | `PATCH /api/embarques/{id}` → actualizar campos editables (notas, fecha_programada, modo_transporte) | embarques:update |
| CA-05 | `POST /api/embarques/{id}/cancelar` → transición a CANCELLED con motivo; valida que no esté DELIVERED | embarques:update |
| CA-06 | `POST /api/embarques/{id}/poner-en-espera` → transición a ON_HOLD con motivo | embarques:update |
| CA-07 | Embarque cancelado → órdenes vuelven a CONFIRMED + limpia shipment_id (desconsolidación automática) | embarques:update |
| CA-08 | `empresa_id` en todos los queries; RLS como segunda capa | — |
| CA-09 | Auditoría registra CREATE y cambios de estado con usuario + motivo | — |
| CA-10 | `DELETE` físico rechazado — soft delete (`activo=false`) vía `PATCH /api/embarques/{id}/cancelar` | — |

### DTOs

```csharp
// Request
public record ShipmentRequestDto(
    Guid? CarrierId,
    Guid? ConductorId,
    Guid? VehiculoId,
    DateTime? FechaProgramada,
    DateTime? FechaEntregaEst,
    Guid? OrigenId,
    Guid? DestinoId,
    string ModoTransporte,       // TERRESTRE|AEREO|MARITIMO|FERROVIARIO|INTERMODAL
    decimal? PesoTotalKg,
    decimal? VolumenTotalM3,
    string? Notas
);

// Response
public record ShipmentResponseDto(
    Guid Id,
    string NumeroShipment,
    string Estado,
    string ModoTransporte,
    Guid? CarrierId,
    string? NombreCarrier,
    Guid? ConductorId,
    string? NombreConductor,
    Guid? VehiculoId,
    string? PlacaVehiculo,
    DateTime? FechaProgramada,
    DateTime? FechaSalidaReal,
    DateTime? FechaEntregaEst,
    DateTime? FechaEntregaReal,
    string? OrigenNombre,
    string? DestinoNombre,
    decimal? PesoTotalKg,
    decimal? VolumenTotalM3,
    int CantidadOrdenes,         // COUNT de ordenes.shipment_id = this.Id
    List<string> TransicionesDisponibles,
    string? Notas,
    DateTime FechaCreacion,
    DateTime FechaModificacion
);

// List DTO (paginado)
public record ShipmentListDto(
    Guid Id,
    string NumeroShipment,
    string Estado,
    string ModoTransporte,
    string? NombreCarrier,
    string? NombreConductor,
    DateTime? FechaProgramada,
    int CantidadOrdenes,
    DateTime FechaCreacion
);
```

---

## HU-034 · Asignación de carrier y conductor

**Como** dispatcher, **quiero** asignar un carrier, conductor y vehículo a un embarque,  
**para** definir quién ejecutará el transporte.

**Módulo de permiso:** `embarques`  
**Estimación:** 8 pts | **Prioridad:** Alta

### Criterios de Aceptación

| CA | Descripción |
|----|-------------|
| CA-01 | `POST /api/embarques/{id}/asignar-carrier` → 200; acepta `carrier_id`, `conductor_id`, `vehiculo_id` |
| CA-02 | Carrier inactivo o bloqueado → 422 BusinessException "El carrier no está disponible para asignación" |
| CA-03 | Conductor sin licencia vigente (carriers.documentos vencidos) → 422 "Conductor no habilitado" |
| CA-04 | Embarque en DELIVERED o CANCELLED → 422 "No se puede reasignar un embarque en estado terminal" |
| CA-05 | Asignación exitosa → estado permanece PLANNED (transición a IN_TRANSIT es acción separada) |
| CA-06 | `POST /api/embarques/{id}/confirmar-salida` → transición PLANNED → IN_TRANSIT + registra `fecha_salida_real = now()` |
| CA-07 | Confirmar salida sin carrier asignado → 422 "Debe asignar un carrier antes de confirmar salida" |
| CA-08 | Auditoría registra asignación: carrier anterior / nuevo, usuario, timestamp |
| CA-09 | `GET /api/carriers/{id}/disponibilidad` → lista embarques activos del carrier (para verificar carga) |

### SQL — Request DTO asignación

```csharp
public record AsignarCarrierRequestDto(
    Guid CarrierId,
    Guid? ConductorId,
    Guid? VehiculoId
);
```

---

## HU-035 · Consolidación real de órdenes en embarque (Fix G-13)

**Como** dispatcher, **quiero** consolidar múltiples órdenes confirmadas en un embarque real,  
**para** optimizar el transporte y que el shipment exista antes de asignarse a las órdenes.

**Módulo de permiso:** `embarques` + `ordenes`  
**Estimación:** 8 pts | **Prioridad:** Alta (contiene Fix G-13 🔴)

### Criterios de Aceptación

| CA | Descripción |
|----|-------------|
| CA-01 | `POST /api/embarques/consolidar` → 201 con shipment creado ANTES de actualizar ordenes (fix G-13) |
| CA-02 | Mínimo 2 órdenes → 422 si se envía menos |
| CA-03 | Todas las órdenes deben estar en estado CONFIRMED → 422 si alguna no lo está |
| CA-04 | Todas las órdenes deben pertenecer a la misma empresa → 422 si se cruzan empresas |
| CA-05 | Orden ya consolidada (shipment_id != null) → 422 "La orden {numero} ya pertenece a un embarque" |
| CA-06 | Flujo atómico: INSERT shipment → UPDATE ordenes → historial → auditoría (todo en transacción) |
| CA-07 | Response incluye: `shipment_id`, `numero_shipment`, `cantidad_ordenes`, lista de `ordenes_consolidadas[]` |
| CA-08 | `POST /api/embarques/{id}/desconsolidar-orden/{ordenId}` → quita una orden del embarque; vuelve a CONFIRMED |
| CA-09 | Desconsolidar última orden del embarque → cancela el embarque automáticamente (0 órdenes = inválido) |
| CA-10 | Historial de estado de cada orden registra evento "Consolidada en embarque {numero_shipment}" |

### SQL — Request consolidación

```csharp
public record ConsolidarEmbarqueRequestDto(
    List<Guid> OrdenIds,
    string? ModoTransporte,      // opcional; default TERRESTRE
    DateTime? FechaProgramada,
    string? Notas
);

public record ConsolidarEmbarqueResponseDto(
    Guid ShipmentId,
    string NumeroShipment,
    string Estado,
    int CantidadOrdenes,
    List<OrdenConsolidadaDto> OrdenesConsolidadas
);

public record OrdenConsolidadaDto(
    Guid OrdenId,
    string NumeroOrden,
    string EstadoAnterior,
    string EstadoNuevo       // siempre ASSIGNED
);
```

---

## HU-036 · Track & Trace básico

**Como** cliente o dispatcher, **quiero** registrar y consultar los eventos de posición y estado de un embarque,  
**para** saber en tiempo real dónde está la carga.

**Módulo de permiso:** `track_trace`  
**Estimación:** 13 pts | **Prioridad:** Alta  
**ADRs:** ADR-014 (Nominatim/Leaflet)

### SQL — Tabla eventos_track_trace

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_eventos_track_trace.sql

CREATE TABLE IF NOT EXISTS eventos_track_trace (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    shipment_id     UUID NOT NULL REFERENCES shipments(id) ON DELETE RESTRICT,
    tipo_evento     VARCHAR(40) NOT NULL,
    descripcion     TEXT,
    latitud         NUMERIC(10,7),
    longitud        NUMERIC(10,7),
    direccion_aprox TEXT,            -- geocodificación inversa Nominatim (ADR-014)
    registrado_por  UUID REFERENCES usuarios(id),
    es_automatico   BOOLEAN NOT NULL DEFAULT false,  -- true = GPS/sistema; false = manual
    fecha_evento    TIMESTAMPTZ NOT NULL DEFAULT now(),
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE eventos_track_trace IS
    'Eventos de rastreo GPS y manual de embarques. Inmutables por diseño — solo INSERT.';

COMMENT ON COLUMN eventos_track_trace.tipo_evento IS
    'SALIDA_ORIGEN | LLEGADA_PARADA | SALIDA_PARADA | LLEGADA_DESTINO | INCIDENCIA | POSICION_GPS | ENTREGA_CONFIRMADA';

CREATE INDEX idx_events_tt_shipment ON eventos_track_trace(shipment_id);
CREATE INDEX idx_events_tt_empresa  ON eventos_track_trace(empresa_id);
CREATE INDEX idx_events_tt_fecha    ON eventos_track_trace(fecha_evento DESC);

ALTER TABLE eventos_track_trace ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_eventos_tt" ON eventos_track_trace
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_eventos_tt_fecha_mod
    BEFORE UPDATE ON eventos_track_trace
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

### Criterios de Aceptación

| CA | Descripción |
|----|-------------|
| CA-01 | `POST /api/track-trace/{shipmentId}/eventos` → 201; acepta tipo_evento, descripcion, latitud, longitud |
| CA-02 | `GET /api/track-trace/{shipmentId}/eventos` → listado cronológico ASC de todos los eventos del embarque |
| CA-03 | Evento con lat/lng → geocodificación inversa Nominatim asíncrona; guarda `direccion_aprox` (ADR-014) |
| CA-04 | Evento `SALIDA_ORIGEN` solo válido si embarque está en PLANNED → lo transiciona a IN_TRANSIT automáticamente |
| CA-05 | Evento `LLEGADA_DESTINO` solo válido si embarque está en IN_TRANSIT |
| CA-06 | Evento `ENTREGA_CONFIRMADA` → transiciona embarque a DELIVERED + registra `fecha_entrega_real = fecha_evento` |
| CA-07 | Eventos son inmutables — no existe PATCH/DELETE sobre eventos_track_trace |
| CA-08 | `GET /api/embarques/{id}/ultima-posicion` → último evento con latitud/longitud para mapa Leaflet |
| CA-09 | shipment en CANCELLED o DELIVERED → 422 al intentar agregar eventos nuevos |
| CA-10 | `es_automatico = false` para eventos manuales vía API; reservar `true` para integración GPS futura |

### DTOs

```csharp
public record EventoTrackTraceRequestDto(
    string TipoEvento,          // SALIDA_ORIGEN|LLEGADA_PARADA|SALIDA_PARADA|LLEGADA_DESTINO|INCIDENCIA|ENTREGA_CONFIRMADA
    string? Descripcion,
    decimal? Latitud,
    decimal? Longitud
);

public record EventoTrackTraceResponseDto(
    Guid Id,
    Guid ShipmentId,
    string TipoEvento,
    string? Descripcion,
    decimal? Latitud,
    decimal? Longitud,
    string? DireccionAprox,
    string? NombreRegistradoPor,
    bool EsAutomatico,
    DateTime FechaEvento
);
```

---

## HU-037 · Documentos de embarque

**Como** operador, **quiero** adjuntar y consultar documentos (carta porte, guías, POD) en un embarque,  
**para** tener la carpeta documental completa del viaje.

**Módulo de permiso:** `documentos`  
**Estimación:** 13 pts | **Prioridad:** Alta  
**ADRs:** ADR-012 (Signed URLs Supabase Storage)

### SQL — Tabla documentos_embarque

```sql
-- supabase/migrations/YYYYMMDDHHMMSS_tabla_documentos_embarque.sql

CREATE TABLE IF NOT EXISTS documentos_embarque (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id      UUID NOT NULL REFERENCES empresas(id) ON DELETE RESTRICT,
    shipment_id     UUID NOT NULL REFERENCES shipments(id) ON DELETE RESTRICT,
    tipo_documento  VARCHAR(40) NOT NULL,
    nombre_archivo  VARCHAR(255) NOT NULL,
    storage_path    TEXT NOT NULL,       -- ruta en Supabase Storage bucket privado
    mime_type       VARCHAR(100),
    tamanio_bytes   BIGINT,
    descripcion     TEXT,
    subido_por      UUID REFERENCES usuarios(id),
    activo          BOOLEAN NOT NULL DEFAULT true,
    fecha_creacion  TIMESTAMPTZ NOT NULL DEFAULT now(),
    fecha_modificacion TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE documentos_embarque IS
    'Documentos adjuntos a embarques almacenados en Supabase Storage (ADR-012).';

COMMENT ON COLUMN documentos_embarque.tipo_documento IS
    'CARTA_PORTE | GUIA_REMISION | POD | MANIFIESTO | POLIZA_SEGURO | OTRO';

COMMENT ON COLUMN documentos_embarque.storage_path IS
    'Ruta interna en bucket privado. Las URLs se generan como signed URLs temporales (ADR-012).';

CREATE INDEX idx_docs_embarque_shipment ON documentos_embarque(shipment_id);
CREATE INDEX idx_docs_embarque_empresa  ON documentos_embarque(empresa_id);
CREATE INDEX idx_docs_embarque_tipo     ON documentos_embarque(tipo_documento);

ALTER TABLE documentos_embarque ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresa_isolation_docs_embarque" ON documentos_embarque
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

CREATE TRIGGER trg_docs_embarque_fecha_mod
    BEFORE UPDATE ON documentos_embarque
    FOR EACH ROW EXECUTE FUNCTION update_fecha_modificacion();
```

### Criterios de Aceptación

| CA | Descripción |
|----|-------------|
| CA-01 | `POST /api/embarques/{id}/documentos` → 201; acepta tipo_documento, nombre_archivo, storage_path, mime_type, descripcion |
| CA-02 | `GET /api/embarques/{id}/documentos` → lista de documentos del embarque (sin signed URL) |
| CA-03 | `GET /api/embarques/{id}/documentos/{docId}/signed-url` → signed URL temporal (15 min) vía Supabase Storage (ADR-012) |
| CA-04 | Tipo de documento requerido: CARTA_PORTE | GUIA_REMISION | POD | MANIFIESTO | POLIZA_SEGURO | OTRO → 422 si valor no válido |
| CA-05 | Soft delete: `DELETE /api/embarques/{id}/documentos/{docId}` → marca activo=false (no borra Storage) |
| CA-06 | Embarque en CANCELLED → 422 al intentar subir documentos nuevos |
| CA-07 | Auditoría registra subida de cada documento (usuario, tipo, shipment_id) |
| CA-08 | `GET /api/embarques/{id}` incluye `total_documentos: N` en el response |
| CA-09 | storage_path debe comenzar con `shipments/{empresa_id}/` → validación BLL |
| CA-10 | tamanio_bytes máximo: 50 MB → 422 si excede "El archivo supera el límite de 50 MB" |

### DTOs

```csharp
public record DocumentoEmbarqueRequestDto(
    string TipoDocumento,
    string NombreArchivo,
    string StoragePath,
    string? MimeType,
    long? TamanioBytes,
    string? Descripcion
);

public record DocumentoEmbarqueResponseDto(
    Guid Id,
    Guid ShipmentId,
    string TipoDocumento,
    string NombreArchivo,
    string? MimeType,
    long? TamanioBytes,
    string? Descripcion,
    string? NombreSubidoPor,
    DateTime FechaCreacion
);

public record SignedUrlResponseDto(
    Guid DocumentoId,
    string SignedUrl,
    DateTime Expira        // UTC — now() + 15 min
);
```

---

## Constantes Utility a agregar (Fase 1)

```csharp
// Utility/Constants/EstadoEmbarque.cs
public static class EstadoEmbarque
{
    public const string Planned    = "PLANNED";
    public const string InTransit  = "IN_TRANSIT";
    public const string Delivered  = "DELIVERED";
    public const string Cancelled  = "CANCELLED";
    public const string OnHold     = "ON_HOLD";

    public static readonly IReadOnlySet<string> EstadosTerminales =
        new HashSet<string> { Delivered, Cancelled };

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Transiciones =
        new Dictionary<string, IReadOnlySet<string>>
        {
            [Planned]   = new HashSet<string> { InTransit, Cancelled },
            [InTransit] = new HashSet<string> { Delivered, OnHold, Cancelled },
            [OnHold]    = new HashSet<string> { InTransit, Cancelled },
            [Delivered] = new HashSet<string>(),
            [Cancelled] = new HashSet<string>(),
        };
}

// Utility/Constants/TipoDocumentoEmbarque.cs
public static class TipoDocumentoEmbarque
{
    public const string CartaPorte   = "CARTA_PORTE";
    public const string GuiaRemision = "GUIA_REMISION";
    public const string Pod          = "POD";
    public const string Manifiesto   = "MANIFIESTO";
    public const string PolizaSeguro = "POLIZA_SEGURO";
    public const string Otro         = "OTRO";

    public static readonly IReadOnlySet<string> Valores =
        new HashSet<string> { CartaPorte, GuiaRemision, Pod, Manifiesto, PolizaSeguro, Otro };
}

// Utility/Constants/TipoEventoTrack.cs
public static class TipoEventoTrack
{
    public const string SalidaOrigen      = "SALIDA_ORIGEN";
    public const string LlegadaParada     = "LLEGADA_PARADA";
    public const string SalidaParada      = "SALIDA_PARADA";
    public const string LlegadaDestino    = "LLEGADA_DESTINO";
    public const string Incidencia        = "INCIDENCIA";
    public const string PosicionGps       = "POSICION_GPS";
    public const string EntregaConfirmada = "ENTREGA_CONFIRMADA";

    // Eventos que disparan transición de estado en el embarque
    public static readonly IReadOnlyDictionary<string, string> TransicionesAutomaticas =
        new Dictionary<string, string>
        {
            [SalidaOrigen]      = EstadoEmbarque.InTransit,
            [EntregaConfirmada] = EstadoEmbarque.Delivered,
        };
}
```

---

## Resumen de endpoints Sprint 6

| Método | Ruta | HU | Permiso |
|--------|------|----|---------|
| POST   | `/api/embarques` | HU-033 | embarques:create |
| GET    | `/api/embarques` | HU-033 | embarques:read |
| GET    | `/api/embarques/{id}` | HU-033 | embarques:read |
| PATCH  | `/api/embarques/{id}` | HU-033 | embarques:update |
| POST   | `/api/embarques/{id}/cancelar` | HU-033 | embarques:update |
| POST   | `/api/embarques/{id}/poner-en-espera` | HU-033 | embarques:update |
| POST   | `/api/embarques/{id}/asignar-carrier` | HU-034 | embarques:update |
| POST   | `/api/embarques/{id}/confirmar-salida` | HU-034 | embarques:update |
| GET    | `/api/carriers/{id}/disponibilidad` | HU-034 | embarques:read |
| POST   | `/api/embarques/consolidar` | HU-035 | embarques:create |
| POST   | `/api/embarques/{id}/desconsolidar-orden/{ordenId}` | HU-035 | embarques:update |
| POST   | `/api/track-trace/{shipmentId}/eventos` | HU-036 | track_trace:create |
| GET    | `/api/track-trace/{shipmentId}/eventos` | HU-036 | track_trace:read |
| GET    | `/api/embarques/{id}/ultima-posicion` | HU-036 | track_trace:read |
| POST   | `/api/embarques/{id}/documentos` | HU-037 | documentos:create |
| GET    | `/api/embarques/{id}/documentos` | HU-037 | documentos:read |
| GET    | `/api/embarques/{id}/documentos/{docId}/signed-url` | HU-037 | documentos:read |
| DELETE | `/api/embarques/{id}/documentos/{docId}` | HU-037 | documentos:update |

**Total nuevos endpoints Sprint 6: ~18** (más ~3 endpoints internos de fixes)

---

## Convenciones no negociables (Sprint 6)

```
1. Fix G-13 es CONDICIÓN BLOQUEANTE — Fase 3 no arranca hasta que esté resuelto
2. Módulo permiso EMBARQUES (no SHIPMENTS) — módulo definido en AGENTS.md
3. Módulo permiso TRACK_TRACE para HU-036 (módulo separado de embarques)
4. Módulo permiso DOCUMENTOS para HU-037
5. nombre_completo (no nombre) en tabla usuarios para JOINs
6. Clases CSS: fr-badge-* / fr-btn-* / fr-table (no badge-fr-*)
7. MVC: User.HasPermission() — no [RequireModulePermission]
8. Vistas área Tenant: Detalle.cshtml (no Detail)
9. Validators: ShipmentValidator (no ShipmentRequestValidator)
10. ModoTransporte en EMBARQUES: TERRESTRE|AEREO|MARITIMO|FERROVIARIO|INTERMODAL
    (mismo dominio que ÓRDENES — no mezclar con FTL/LTL de tarifas)
11. PagedResult<T> para listados paginados (ADR-008)
12. Transiciones del embarque como List<string> en JSON (mismo patrón que ordenes)
13. Signed URLs con expiración 15 minutos (ADR-012)
14. Eventos track_trace son INMUTABLES — no PATCH/DELETE
15. BusinessException → 422 siempre
16. Create → 201 con CreatedAtAction siempre
17. Soft delete en documentos_embarque (activo=false), nunca DELETE físico
```

---

## Notas de arquitectura para agentes

### @Arquitecto (Fase 1)
- La entidad `Shipment` ya existe como esqueleto en Sprint 4 — **expandir**, no recrear
- `ShipmentStateMachine` análoga a `OrderStateMachine` (ADR-019) — crear nueva clase
- Interfaces DAL: `IShipmentRepository` (expand existing), `IEventoTrackTraceRepository` (nuevo), `IDocumentoEmbarqueRepository` (nuevo)
- No crear ADR nuevo a menos que haya decisión arquitectónica no cubierta por ADR-001 a ADR-020

### @IngenieroDatos (Fase 2)
- Aplicar `ALTER TABLE shipments` (no DROP/CREATE) — la tabla ya tiene datos de tests anteriores
- Fix G-18: solo tocar el método `GetAllAsync` de `OrdenRepository` — no reescribir el repositorio
- RLS en tablas nuevas: copiar patrón de `historial_estados_orden` exactamente

### @BackendDev (Fase 3)
- Fix G-13 PRIMERO, antes de escribir `ShipmentService` nuevo
- Geocodificación Nominatim en `TrackTraceService.RegistrarEventoAsync` debe ser **fire-and-forget** o asíncrono tolerante a fallos (timeout 2s, si falla → `direccion_aprox = null`, no lanzar excepción)
- Signed URL vía `IStorageService` (ya existe del ADR-012) — no reimplementar
- `DocumentoEmbarqueService.SubirDocumentoAsync` valida storage_path (prefijo `shipments/{empresa_id}/`) en BLL antes de guardar

### @FrontendDev (Fase 5)
- Mapa Track & Trace: usar Leaflet.js CDN (ya en el proyecto por ADR-014) — no importar nueva librería
- Vista Detalle del embarque: incluir tab "Eventos" (timeline) + tab "Documentos" + tab "Órdenes"
- Botón "Subir documento": `<input type="file">` sin `<form>` — usar `fetch` con `FormData` directamente
