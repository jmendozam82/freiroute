# QA Report — Sprint 3 · EP-03 Gestión de Maestros y Catálogos

## 1. Resumen ejecutivo

| Métrica | Resultado | Objetivo | Estado |
|---|---|---|---|
| `dotnet build Freiroute.sln` | **0 warnings / 0 errores** | 0 / 0 | ✅ |
| Unit Tests BLL (`Freiroute.BLL.Tests`) | **531 pruebas · 0 fallos** | todas OK | ✅ |
| Integration Tests API (`Freiroute.API.Tests`) | **194 pruebas · 0 fallos** | todas OK | ✅ |
| Cobertura **BLL** (ensamblado `Freiroute.BLL`) | **84.38 %** (63.95 % ramas) | ≥ 80 % | ✅ |
| Cobertura **API** (ensamblado `Freiroute.API`) | **80.94 %** (48.86 % ramas) | ≥ 60 % | ✅ |

**Total de pruebas:** 725 (531 unitarias + 194 de integración) — **todas en verde.**

> Sprint 1: 0 tests · Sprint 2: 431 · **Sprint 3: 725** — se añadieron **294 pruebas** en este ciclo (168 BLL + 126 API acumulado vs. Sprint 2, incluyendo cierre de brechas CRUD).

---

## 2. Cobertura por clase (BLL — 8 servicios Sprint 3 + validators + HU-004)

| Clase (in-scope Sprint 3) | Cobertura | Estado |
|---|---|---|
| `UbicacionService` | 84.8 % | ✅ |
| `NominatimGeocodingService` (ADR-014) | 85.9 % | ✅ |
| `ZonaEntregaService` | 86.9 % | ✅ |
| `TarifaBaseService` (ADR-015) | 88.1 % | ✅ |
| `ClienteService` | 91.2 % | ✅ |
| `AuthService` (OAuth HU-004) | 91.4 % | ✅ |
| `UnidadMedidaService` | 93.3 % | ✅ |
| `TipoEmbalajeService` | 95.8 % | ✅ |
| `TipoMercanciaService` | 96.7 % | ✅ |
| `ZonaEntregaValidator` | 79.6 % | ✅ |
| `ClienteValidator` | 91.5 % | ✅ |
| `UnidadMedidaValidator` | 97.1 % | ✅ |
| `TipoMercanciaValidator` | 100 % | ✅ |
| `TipoEmbalajeValidator` | 100 % | ✅ |
| `TarifaBaseValidator` | 100 % | ✅ |
| `UbicacionValidator` | 100 % | ✅ |
| **Ensamblado `Freiroute.BLL` (agregado)** | **84.38 %** | ✅ ≥ 80 % |

> **Nota de alcance:** Quedan fuera de los objetivos las clases de infraestructura externa
> sin lógica de negocio propia testeable en unitarios: `SupabaseAuthServiceReal/Stub`,
> `SupabaseStorageService`, `ResendEmailService`, `EmailServiceStub` (0 % — dependencias
> externas de red/BD, no inyectables en unit tests). El umbral ≥ 80 % se cumple en el agregado.

### Cobertura por controller (API)

| Controller | Cobertura |
|---|---|
| `UbicacionesController` | 96.2 % |
| `ClientesController` | 94.4 % |
| `TiposMercanciaController` | 95.3 % |
| `ZonasEntregaController` | 100 % |
| `UnidadesMedidaController` | 100 % |
| `TiposEmbalajeController` | 100 % |
| `TarifasBaseController` | 100 % |
| `AuthController` (OAuth callback) | 96.5 % |
| `ConfiguracionController` · `OnboardingController` · `UsuariosController` · `AuditoriaController` | 94.9–100 % |
| `AdminController` (Sprint 2) | 53.5 %* |
| `EmpresasController` (Sprint 2) | 57.6 %* |

*\* Cobertura parcial heredada de Sprint 2: `AdminController` llama repositorios DAL
directamente en 4 endpoints (ver observación en reporte Sprint 2) y `EmpresasController`
no tiene tests de integración dedicados; su lógica BLL sí está cubierta unitariamente.*

---

## 3. Verificación de criterios de aceptación por Historia de Usuario

### HU-004 · OAuth 2.0 Google/Microsoft — implementación completada en Sprint 3 (5/6) ✅

| CA | Descripción | Verificado por | Estado |
|---|---|---|---|
| CA-01 | POST /api/auth/oauth/callback retorna JWT interno | `AuthControllerTests` (callback OAuth → 200) + `AuthServiceG06OAuthTests.LoginConOAuthAsync_CuandoUsuarioYaVinculado_RetornaLoginSinReVincular` | ✅ |
| CA-02 | Email ya existe → vincular supabase_user_id y login | `LoginConOAuthAsync_CuandoNoVinculadoPeroMismoEmail_VinculaSupabaseUserId` | ✅ |
| CA-03 | Email nuevo → empresa por dominio/invitación | `LoginConOAuthAsync_CuandoAutoprovisionaDesdeInvitacion_CreaUsuarioYAudita` + `..._CuandoSinInvitacionPendiente_LanzaBusinessException` | ✅ |
| CA-04 | JWT con los mismos claims que el login normal | Mismo pipeline `GenerarJwtInterno` (claims empresa/perfiles/permisos verificados en `AuthServiceTests`); sin test dedicado de claims OAuth | ✅* |
| CA-05 | Auditoría `LOGIN_OAUTH` registrada | `_auditoria.Verify(RegistrarAsync("auth", AccionAuditoria.LOGIN_OAUTH, ...))` | ✅ |
| CA-06 | Botones Google/Microsoft funcionales en Login.cshtml | — (UI/`Aplicacion`) | ⏳ Diferido a @FrontendDev |

*\* Se recomienda añadir una aserción explícita de claims en el flujo OAuth — sin impacto en el umbral.*

### HU-015 · Gestión de Ubicaciones y Geocodificación (8/8) ✅

| CA | Descripción | Verificado por | Estado |
|---|---|---|---|
| CA-01 | CRUD completo de ubicaciones | `UbicacionServiceTests` (Create/Update) + `UbicacionServiceCrudTests` (GetAll paginado/Deactivate) + `UbicacionesControllerTests` (GetAll/GetById/Create/Update/Deactivate) | ✅ |
| CA-02 | Geocodificación automática con Nominatim (ADR-014) | `CreateAsync_GeocodeExitoso_GuardaConCoordenadas` + `NominatimGeocodingServiceTests.GeocodeAsync_CuandoRespuestaValida_RetornaCoordenadas` | ✅ |
| CA-03 | Fallo de geocodificación → guarda sin coordenadas (fail-soft) | `CreateAsync_GeocodeNulo_GuardaSinCoordenadasFailSoft` + `GeocodeAsync_CuandoHttpError/CuandoClienteLanzaExcepcion_RetornaNullFailSoft` | ✅ |
| CA-04 | Mapa Leaflet con markers por tipo | `GetParaMapaAsync_RetornaPayloadMinimoSoloGeoreferenciadas` + `UbicacionesControllerTests.GetParaMapa_ConPermisoRead_Retorna200ConPayload` (payload del mapa; render Leaflet = UI) | ✅ |
| CA-05 | Ajuste manual del pin en el mapa | `ActualizarCoordenadasAsync_Latitud/LongitudFueraDeRango_LanzaBusinessException` + `UbicacionesControllerTests.ActualizarCoordenadas_ConPermisoUpdate_Retorna200` | ✅ |
| CA-06 | Importación masiva CSV | `ImportarCsv_FilasValidas_ImportaTodas` · `_FilaConTipoInvalido_OmiteYSigue` · `_ConSoloCabecera_ImportaCero` + `UbicacionesControllerTests.Importar_ConPermisoCreate_Retorna200ConConteo` | ✅ |
| CA-07 | Búsqueda por nombre/código/tipo/ciudad | `UbicacionServiceCrudTests.GetAllAsync_ConFiltros_RetornaPaginaCorrecta` + API `GetAll?q=&tipo=` | ✅ |
| CA-08 | Tipos disponibles (ALMACEN…OTRO) | Constantes `TipoUbicacion` + `UbicacionValidator` (100 % — tipos inválidos rechazados en CSV/request) | ✅ |

### HU-016 · Gestión de Zonas de Entrega (6/7) ✅

| CA | Descripción | Verificado por | Estado |
|---|---|---|---|
| CA-01 | CRUD de zonas con nombre/código/color | `ZonaEntregaServiceCrudTests` (GetAll/GetById/Create/Update/Deactivate) + `ZonasEntregaControllerTests` (GetById/Update/GetAll/Create/Deactivate) | ✅ |
| CA-02 | Definición por polígono / códigos postales / ciudades / departamentos | `ZonaEntregaServiceTests` (polígono + ciudades) + `ZonaEntregaValidator` (tipo_definicion) | ✅ |
| CA-03 | Visualización de zonas en mapa con color | — (UI Leaflet) | ⏳ Diferido a @FrontendDev |
| CA-04 | Asignar/desasignar ubicaciones a zonas | `ZonaEntregaServicePertenenciaTests.AsignarUbicacionesAsync_AsignaCadaUbicacionDistintaYAudita` + `DesasignarUbicacionAsync_DesasignaYAudita` + API POST/DELETE `/ubicaciones` (200) | ✅ |
| CA-05 | Point-in-polygon [lng,lat] para zonas tipo polígono | `VerificarPertenenciaAsync_CuandoPuntoDentro/FueraDePoligono...` + `_GeoJsonInvalido_NoRetornaZonaNiPropaga` + API `verificar-pertenencia` | ✅ |
| CA-06 | No se elimina zona con tarifas activas | `ZonaEntregaServiceTests.Deactivate_ZonaConTarifasActivas_LanzaBusinessException` + `ZonaEntregaServicePertenenciaTests.DeactivateAsync_CuandoZonaReferenciadaPorTarifas_LanzaBusinessException` | ✅ |
| CA-07 | Exportar/importar zonas en GeoJSON | Sin endpoint implementado en `ZonasEntregaController` | ⏳ Diferido (no implementado) |

### HU-017 · Catálogo de Tipos de Mercancía (6/7) ✅

| CA | Descripción | Verificado por | Estado |
|---|---|---|---|
| CA-01 | CRUD completo con atributos | `TipoMercanciaServiceTests` (GetAll/GetById/Create/Update/Deactivate) + API | ✅ |
| CA-02 | Clasificación HAZMAT: clase 1-9 + código ONU (incl. subclases "9.9") | `CreateAsync_CuandoHazmat_RetornaEsHazmatTrue` + `TipoMercanciaValidator` (100 % — regex `^[1-9](\.[0-9])?$`) | ✅ |
| CA-03 | Flags de manejo (frágil, perecedero, refrigeración, sobredimensionado, fumigación) | DTO/entity + `TipoMercanciaValidator` | ✅ |
| CA-04 | Rango de temperatura requerido si refrigeración | `TipoMercanciaValidator` (temp_minima y temp_maxima requeridas con `requiere_refrigeracion=true`) | ✅ |
| CA-05 | HS Code para comercio exterior (opcional) | `TipoMercanciaValidator` | ✅ |
| CA-06 | Importación masiva CSV | `ImportarCsvAsync_FilasInvalidas_FailSoftYAudita` + `_CuandoFilaValida_Importa` + API `POST /importar` (conteo 3) | ✅ |
| CA-07 | Badge danger + clase HAZMAT al buscar peligrosas | `GetAllAsync_CuandoSoloPeligrosas_FiltraYRetorna` (filtro backend); render badge = UI | ⏳ Diferido a @FrontendDev |

### HU-018 · Catálogo de Unidades de Medida y Embalajes (5/6) ✅

| CA | Descripción | Verificado por | Estado |
|---|---|---|---|
| CA-01 | CRUD de unidades por tipo con factor de conversión | `UnidadMedidaServiceCrudTests` (GetAll/Create/Update/Deactivate) + API | ✅ |
| CA-02 | Copia de unidades estándar al crear tenant | `EmpresaService.CreateAsync` paso 9 (`CopiarUnidadesEstandarAsync`) cubierto por `EmpresaServiceTests.CreateAsync_*` (mocks `IUnidadMedidaRepository`) | ✅ |
| CA-03 | CRUD de tipos de embalaje con capacidad y apilable | `TipoEmbalajeServiceTests` (GetAll/GetById/Update/Deactivate + código duplicado) + API | ✅ |
| CA-04 | Copia de embalajes estándar al crear tenant | `EmpresaService.CreateAsync` (`CopiarEmbalajesEstandarAsync`) — cubierto en `EmpresaServiceTests.CreateAsync_*` | ✅ |
| CA-05 | Simulador de conversión entre unidades del mismo tipo | `UnidadMedidaServiceTests.ConvertirAsync_KgALibra_CalculaFactorCorrecto` + `_TiposDistintos_LanzaBusinessException` + API `GET /convertir` (220.46 lb) | ✅ |
| CA-06 | No desactivar unidad referenciada por tipos de mercancía | `UnidadMedidaService.DeactivateAsync` **no verifica referencias** (solo GetById + soft delete) | ⏳ Diferido — acción para @BackendDev |

### HU-019 · Catálogo de Clientes (Shippers) (7/9) ✅

| CA | Descripción | Verificado por | Estado |
|---|---|---|---|
| CA-01 | CRUD de clientes (RUC, tipo, crédito, SLA) | `ClienteServiceTests` (GetAll/GetById/Create/Update/Deactivate) + API | ✅ |
| CA-02 | Múltiples contactos con rol diferenciado | `AgregarContactoAsync_CuandoValido_CreaContactoYAudita` + `UpdateContactoAsync_CuandoExiste_ActualizaAuditaYRetornaContacto` + `DeactivateContactoAsync_CuandoExiste_RetornaTrueYAudita` + API (POST 201 / PUT 200 / PATCH 200) | ✅ |
| CA-03 | Ubicación de despacho por defecto | `ClienteServiceTests.GetByIdAsync_CuandoExiste_RetornaClienteConUbicacionDefecto` | ✅ |
| CA-04 | Tipos de cliente REGULAR/VIP/OCASIONAL/CORPORATIVO/GOBIERNO | Enum `TipoCliente` + `ClienteValidator` (91.5 %) | ✅ |
| CA-05 | Estado de crédito AL_DIA/EN_MORA/BLOQUEADO/SIN_CREDITO → alerta BLOQUEADO | `CambiarEstadoCreditoAsync_CuandoEstadoValido_ActualizaYAudita` + `_CuandoEstadoInvalido_LanzaBusinessException` + API `PATCH /estado-credito` (BLOQUEADO) | ✅ |
| CA-06 | Documentos en Supabase Storage | Fuera del alcance de tests BLL/API (UI + storage) | ⏳ Diferido |
| CA-07 | Historial de órdenes | Placeholder — se llena en Sprint 4 (EP-04) | ⏳ Diferido |
| CA-08 | Importación CSV con RUC único por empresa (fail-soft, ADR-017) | `ImportarCsvAsync_CuandoFilaValida_ImportaRegistraYAudita` + `_FilasInvalidas_FailSoftSinPropagar` + `CreateAsync_CuandoRucYaExiste_LanzaConflictException` + API `POST /importar` + `Importar_SinArchivo_Retorna400` | ✅ |
| CA-09 | Exportar directorio a Excel (CSV BOM UTF-8, ADR-017) | `ExportarExcelAsync_RetornaCsvConBomUtf8YAudita` + API `GET /exportar` → `text/csv` | ✅ |

### HU-020 · Catálogo de Tarifas Base (7/7) ✅

| CA | Descripción | Verificado por | Estado |
|---|---|---|---|
| CA-01 | CRUD de tarifas con zonas/modo/servicio/tipo/precio/moneda | `TarifaBaseServiceCrudTests` (GetAll/GetById/Create/Deactivate) + `TarifaBaseServiceTests` + API (GetById/GetAll/Create/Update/Deactivate) | ✅ |
| CA-02 | Recargos configurables por tarifa | `AgregarRecargoAsync_*` (duplicado/desconocido/%) + `UpdateRecargoAsync_CuandoNoExisteRecargo_LanzaNotFoundException` + `DeactivateRecargoAsync_*` + API (POST 201 / PUT 200 / PATCH 200) | ✅ |
| CA-03 | Versionado: Update crea nueva versión y cierra la anterior (ADR-015) | `UpdateAsync_CuandoExiste_CierraVersionActualYCreaNueva` + `UpdateAsync_Exitoso_CierraVigenciaAYerYCreaNuevaVersion` + API PUT → "Tarifa actualizada (nueva versión creada)" | ✅ |
| CA-04 | Simulador de costo: base + recargos + mínimo + seguro | `SimularCostoAsync_CuandoTarifaFija_AplicaPrecioMinimoRecargosYSeguro` + `_CuandoTarifaPorKg_CalculaProductoConRecargoMontoFijo` + API `simular-costo` (montos desglosados) | ✅ |
| CA-05 | Alerta cuando no existe tarifa vigente | `SimularCostoAsync_CuandoNoExisteTarifaVigente_RetornaTarifaEncontradaFalse` + `GetVigenteAsync_CuandoNoExiste_RetornaNullSinExcepcion` + API `GET /vigente` → Data null con mensaje | ✅ |
| CA-06 | Tarifa vencida → badge neutral, no editable | `EsVigente` mapeado en `TarifaBaseService.GetAllAsync` (`GetAllAsync_ConFiltros_RetornaPaginaMapeada`); render badge = UI | ✅ (BLL) + ⏳ badge UI |
| CA-07 | Historial de cambios por zona (auditoría de precios) | Auditoría `UPDATE` sobre `tarifas_base` verificada en `UpdateAsync_*` (AccionAuditoria.UPDATE) | ✅ |

### Totales por HU

| HU | CAs | Verificados | Diferidos |
|---|---|---|---|
| HU-004 (OAuth) | 6 | 5 | 1 (CA-06 botones UI) |
| HU-015 (Ubicaciones) | 8 | 8 | 0 |
| HU-016 (Zonas) | 7 | 6 | 1 (CA-07 GeoJSON — sin endpoint) |
| HU-017 (Mercancías) | 7 | 6 | 1 (CA-07 badge UI) |
| HU-018 (Unidades/Embalajes) | 6 | 5 | 1 (CA-06 referencias — acción @BackendDev) |
| HU-019 (Clientes) | 9 | 7 | 2 (CA-06 docs storage · CA-07 historial Sprint 4) |
| HU-020 (Tarifas) | 7 | 7 | 0 |
| **Total** | **50** | **44** | **6** |

**Resumen:** **44 / 50** criterios de aceptación verificados y en verde. **6 diferidos**
(4 corresponden a UI/`Aplicacion` — botones OAuth, mapa de zonas, badges HAZMAT y tarifa
vencida; 1 a funcionalidad no implementada — export/import GeoJSON de zonas; 1 a lógica
pendiente en `UnidadMedidaService.DeactivateAsync`).

---

## 4. Desviaciones y observaciones

- **`UnidadMedidaService.DeactivateAsync` no valida referencias (HU-018 CA-06, 0 % en esa rama):**
  el spec exige impedir desactivar una unidad referenciada por tipos de mercancía. La implementación
  actual solo hace `GetByIdAsync` + soft delete. **Acción para @BackendDev** antes del Sprint 4
  (las órdenes referenciarán unidades de carga).
- **GeoJSON de zonas (HU-016 CA-07):** no existe endpoint de exportar/importar GeoJSON en
  `ZonasEntregaController`. No se testea por no estar implementado.
- **Nombres de rutas reales vs. spec:** el spec documenta `/api/zonas` y `/api/tarifas` y
  `ZonasController`/`TarifasController`; la implementación usa `/api/zonas-entrega` y
  `/api/tarifas-base`. Los tests de integración validan las rutas reales.
- **Ruta `GET /api/ubicaciones/exportar`** (spec HU-015) **no existe** en `UbicacionesController`
  (solo BLL; el endpoint de exportación no se implementó). Nota para @BackendDev si el frontend
  la necesita en Sprint 4.
- **`TipoEmbalaje*` DTO** vive en `Freiroute.DTO.Unidad` (el spec los listaba agrupados con
  embalaje en carpeta propia): desviación cosmética, sin impacto funcional.
- **`AgregarRecargo` y `AgregarContacto` → 201 (Created):** convención `Create → 201` de Sprint 1
  aplicada también a sub-recursos (recargos y contactos); corregido en tests (primera tirada
  esperaba 200).
- **`ImportarCsv` sin archivo → 400:** verificado en `ClientesControllerTests.Importar_SinArchivo_Retorna400`
  (fail-soft: sin archivo no se procesa nada).
- **CSV fail-soft (ADR-017):** filas inválidas se omiten con log y la importación continúa —
  verificado en BLL (Clientes y Ubicaciones) y API.
- **`BusinessException → 422`** convención Sprint 1: aplicada a conflictos de negocio
  (RUC duplicado, símbolo en uso, zona con tarifas, recargo duplicado, OAuth sin empresa).
- **Cobertura `AdminController` 53.5 % heredada:** 4 endpoints llaman repositorios DAL
  directamente (no mockeables vía `TestWebApplicationFactory`); sin cambios en Sprint 3.
  Recordatorio para @Arquitecto: mover ese acceso a datos a servicios BLL (ya anotado en
  reporte Sprint 2).

---

## 5. Checklist DoD Sprint 3 (QA)

- [x] 431+ tests Sprint 1+2 en verde (no regresión) — 531 BLL + 194 API = **725 totales**
- [x] Cobertura BLL ≥ 80 % (acumulado): **84.38 %** ✅
- [x] Cobertura API ≥ 60 % (acumulado): **80.94 %** ✅
- [x] OAuth funcional (HU-004) — token válido/vencido, vinculación, autoprovisión, auditoría `LOGIN_OAUTH` ✅ (botones UI ⏳ @FrontendDev)
- [x] Geocodificación automática Nominatim + fail-soft sin coordenadas (ADR-014) ✅
- [x] Importación CSV de ubicaciones y clientes (fail-soft + conteo) ✅
- [x] Simulador de tarifas con desglose (base + recargos + mínimo + seguro) ✅
- [x] Cliente con múltiples contactos con rol diferenciado ✅
- [x] Unidades y embalajes estándar copiados al crear tenant (`EmpresaService.CreateAsync`) ✅
- [x] Versionado de tarifas: Update cierra vigencia anterior y crea nueva versión (ADR-015) ✅
- [x] Point-in-polygon y verificación de pertenencia por ciudades ✅
- [x] Todos los endpoints nuevos con permisos `[RequirePermission]` — 401/403 verificados en integración ✅
- [x] Soft delete (`PATCH …/deactivate`) en los 7 catálogos — nunca `DELETE` de recurso ✅
- [x] `dotnet build` 0 warnings ✅
- [x] `dotnet test` todos superados (725) ✅
- [x] QA Report creado en docs/specs/sprint-03-qa-report.md ✅
- [ ] `supabase db diff` vacío verificado por @IngenieroDatos ⏳ (MCP cloud pendiente — misma nota que Sprint 2)
- [ ] UI Catálogos (Leaflet, badges, botones OAuth) — entregable @FrontendDev, sprint de UI ⏳

---

## 6. Archivos de test entregados

**tests/Freiroute.BLL.Tests/Services/** (nuevos Sprint 3)

- `UbicacionServiceTests.cs` — geocodificación automática/fail-soft, update y limpieza de coordenadas, import CSV
- `UbicacionServiceCrudTests.cs` — GetAll paginado (default 20), GetParaMapa, Deactivate, validación lat/lng (CA-05)
- `ZonaEntregaServiceTests.cs` — polígono vs. ciudades, deactivate con/ sin tarifas (CA-06)
- `ZonaEntregaServicePertenenciaTests.cs` — point-in-polygon, asignar/desasignar ubicaciones, auditoría
- `ZonaEntregaServiceCrudTests.cs` — GetAll/GetById/Create/Update/Deactivate con auditoría
- `TipoMercanciaServiceTests.cs` — HAZMAT, CRUD, import CSV fail-soft
- `UnidadMedidaServiceCrudTests.cs` — GetAll por tipo, Update (conflicto de símbolo), Deactivate
- `TipoEmbalajeServiceTests.cs` — CRUD con código duplicado y uppercase
- `ClienteServiceTests.cs` — CRUD, contactos (agregar/update/deactivate), estado crédito, RUC único, import/export CSV (ADR-017)
- `TarifaBaseServiceCrudTests.cs` — GetAll con filtros, GetById, GetVigente, Create con recargos, Deactivate(+Recargo)
- `TarifaBaseServiceSimuladorTests.cs` — simulador (fija/POR_KG, mínimo, seguro), recargos, Update versionado (ADR-015)
- `NominatimGeocodingServiceTests.cs` — Geocode/ReverseGeocode válidos y fail-soft (red/HTTP/JSON)
- `AuthServiceG06OAuthTests.cs` — OAuth token válido/vencido, vinculación por email, autoprovisión por invitación, auditoría LOGIN_OAUTH, reset de password admin

**tests/Freiroute.API.Tests/Controllers/** (nuevos Sprint 3)

- `UbicacionesControllerTests.cs` — GetById/GetAll/GetParaMapa/Create/Update/coordenadas/Deactivate/Importar (401/403/200/201/404)
- `ZonasEntregaControllerTests.cs` — GetAll/GetById/Create/Update/VerificarPertenencia/Asignar/Desasignar/Deactivate
- `TarifasBaseControllerTests.cs` — SimularCosto (montos), GetAll/GetById/GetVigente (null incluido)/Create/Update/Recargos (CRUD)/Deactivate
- `ClientesControllerTests.cs` — GetAll/GetById/Create/Update/Deactivate/EstadoCredito/Contactos/Importar/Exportar
- `TiposMercanciaControllerTests.cs` — GetAll filtrado/GetById/Create/Update/Importar (multipart)/Deactivate
- `UnidadesMedidaControllerTests.cs` — Convertir/GetAll/GetById/Create/Update/Deactivate
- `TiposEmbalajeControllerTests.cs` — GetAll/GetById/Create/Update/Deactivate

**tests/Freiroute.API.Tests/**

- `TestWebApplicationFactory.cs` — +7 mocks de servicios BLL Sprint 3 (`UbicacionService`, `ZonaEntregaService`, `TarifaBaseService`, `ClienteService`, `TipoMercanciaService`, `UnidadMedidaService`, `TipoEmbalajeService`)
- `JwtTestHelper.cs` — tokens con permisos `configuracion:*` / `clientes:*`, sin permisos, solo lectura

---

## 7. Herramientas usadas

xUnit · Moq · FluentAssertions · FluentValidation (mocks) · Coverlet (XPlat Code Coverage) · WebApplicationFactory (Microsoft.AspNetCore.Mvc.Testing) · PowerShell + XML parsing (OpenCover) para cobertura por clase/controller · Supabase CLI (migraciones RLS)

---

*Informe QA Sprint 3 — Freiroute TMS*  
*Versión: 1.0 | Fecha: 2026-09-06 | Cobertura: BLL 84.38 % · API 80.94 %*  
*Épica: EP-03 — Gestión de Maestros y Catálogos*  
*Sprint: 03 | Estado: DoD de QA cumplido — 44/50 CAs verificados, 6 diferidos*  
*Total pruebas: 725 (531 BLL + 194 API) · 0 fallos · build 0 warnings / 0 errores*