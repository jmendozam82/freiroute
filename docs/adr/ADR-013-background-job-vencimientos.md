# ADR-013: Background Job de Vencimientos con IHostedService

## Estado
Aceptado

## Fecha
2026 — Sprint 2

## Contexto
La facturación manual (HU-011) requiere un proceso automático que:
1. Pase las suscripciones `ACTIVE` vencidas a `PAST_DUE` (período de gracia).
2. Pase las `PAST_DUE` con más de 7 días a `SUSPENDED`.
3. Envíe alertas al Super Admin (15, 7 y 1 día antes del vencimiento).
4. Purgue los códigos 2FA temporales vencidos (HU-005).
El spec pide usar `IHostedService` o `BackgroundService` de .NET sin Hangfire
en Sprint 2.

## Decisión
Se implementa un **BackgroundService** (`VencimientoSuscripcionJob`) heredando
de `BackgroundService` de .NET, que ejecuta el procesamiento **diariamente a
las 00:00 UTC** usando un `System.Threading.Timer` / `PeriodicTimer` con
cálculo del delay hasta la próxima medianoche UTC.

- Se registra en `IHostedService` dentro del proyecto `Freiroute.API`.
- La lógica de negocios delega en `ISuscripcionService.ProcesarVencimientosAsync()`
  (la BLL no conoce el scheduling — solo ejecuta el proceso).
- Se usa `PeriodicTimer` (available en .NET 6+) para no disparar intervalos
  superpuestos si una pasada tarda más que el período.

## Alternativas Consideradas

1. **Hangfire** — Descartada para Sprint 2 (regla del spec): requiere una
   tabla adicional y almacenamiento de jobs, complejidad innecesaria para un
   job diario simple. Se evalúa en Sprint 3+ si aparecen más jobs
   (geofences, track & trace, etc.).

2. **Cron externo (Windows Task Scheduler / cron + curl)** — Descartada porque
   acopla el scheduling al host y no escala de forma portable entre CI/dev/cloud.

3. **Timer simple `Threading.Timer`** — Descartada por riesgo de solapamiento;
   se prefiere `PeriodicTimer` que no re-dispara hasta completar la iteración.

## Consecuencias

**Positivas:**
- Sin dependencias externas ni tablas extra — el job corre dentro de la API.
- Reintroducción sencilla y portable entre entornos.
- `PeriodicTimer` evita carreras entre iteraciones.

**Negativas / Trade-offs:**
- Si la API se apaga, el job no corre (aceptable — el vencimiento se puede
  reprocesar al arrancar o con un fix manual del Super Admin).
- En multi-instancia podrían duplicarse ejecuciones (mitigado por idempotencia
  de `ProcesarVencimientosAsync` y transiciones de estado).

## Módulos Afectados
- `Freiroute.API/BackgroundJobs/VencimientoSuscripcionJob.cs` (nuevo)
- `ISuscripcionService.ProcesarVencimientosAsync()`
- `IConfiguracion2faRepository.PurgarCodigosExpiradosAsync()`
