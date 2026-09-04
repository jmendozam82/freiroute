# ADR-012: Signed URLs para Logos en Supabase Storage

## Estado
Aceptado

## Fecha
2026 — Sprint 2

## Contexto
En el onboarding (HU-012) y la configuración del tenant (HU-014) se sube y
muestra el logo de cada empresa. El spec define que los buckets de Supabase
Storage son PRIVADOS (regla AGENTS.md #32). Para que el navegador del Admin
pueda ver el logo desde la UI sin exponer el bucket al público, se necesita
un mecanismo de acceso temporal y firmado.

## Decisión
Los logos se almacenan en el bucket privado `logos-tenants` y se sirven
mediante **signed URLs temporales** generadas con el SDK de Supabase:

- Path: `{empresa_id}/logo.{ext}` (PNG o SVG, máx 2 MB).
- Al mostrar el logo en la UI se genera una signed URL con expiración de
  **24 horas**.
- La URL firmada NUNCA se persiste — se genera on-demand al servir la
  configuración (`ConfiguracionResponseDto.LogoUrl` es siempre una signed URL
  fresca).
- Al subir un logo nuevo se sobrescribe el objeto (invalida el anterior).

## Alternativas Consideradas

1. **Bucket público** — Descartada: el logo es un dato de negocio del tenant
   (podría no querer publicarse) y contradice la regla de buckets privados.

2. **Persistir la signed URL en BD** — Descartada porque las signed URLs
   expiran; guardar una URL que caduca obliga a regenerarla. Mejor generarla
   on-demand siempre fresca.

3. **CDN público gestionado** — Descartada en esta fase por la configuración
   adicional de Supabase Storage y por el costo. La carga de logos es baja.

## Consecuencias

**Positivas:**
- Los logos permanecen privados — solo se accede mediante token temporal.
- La URL siempre es fresca; no hay URLs vencidas cacheadas en la BD.
- Aplica el mismo patrón a futuros documentos/POD del tenant.

**Negativas / Trade-offs:**
- Cada request que sirve la configuración genera una signed URL (costo mínimo
  de cómputo en Supabase, aceptable).
- El cliente debe re-fetch de la configuración para refrescar logos vencidos.

## Módulos Afectados
- `IConfiguracionRepository` (logo_url)
- `IConfiguracionService.UpdateLogoAsync` / `DeleteLogoAsync`
- `IOnboardingService.GuardarLogoAsync`
- Supabase Storage bucket `logos-tenants`
