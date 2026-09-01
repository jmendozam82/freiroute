# ADR-001: Stack tecnológico ASP.NET Core + Supabase + Dapper

## Estado
✅ **Aceptado**

## Fecha
2026-08-31

## Contexto
Se requiere definir el stack tecnológico base sobre el cual se construirá todo el sistema SaaS Freiroute TMS. Las opciones consideradas incluyen ORMs mapeadores automáticos (Entity Framework Core), bases de datos cloud nativas (Firebase, Neon) o soluciones híbridas con infraestructura auto-gestionada. El sistema necesita soportar arquitectura multi-tenant a escala mundial comparable a Oracle TMS, SAP TM y BluJay.

## Decisión
El sistema USARÁ exactamente:
- **ASP.NET Core MVC (.NET 8)** para la capa de presentación web
- **ASP.NET Core Web API (.NET 8)** para el backend REST
- **Supabase (PostgreSQL 15)** como Base de Datos relacional y Backend-as-a-Service
- **Dapper** como micro-ORM para consultas SQL directas y altamente performantes
- **Supabase Auth + JWT** para autenticación sin estado y sesiones
- **Serilog** para logging estructurado en JSON

Justificación principal: Dapper ofrece control directo sobre queries SQL (crítico para RLS y optimización multi-tenant) mientras ASP.NET Core proporciona tipado fuerte, inyección de dependencias nativa y un ecosistema maduro de librerías profesionales. Supabase entrega PostgreSQL completo con capacidades BaaS incluidas (Auth, Storage, Realtime).

## Alternativas Consideradas
1. **Entity Framework Core** — Descartado porque agrega una capa de abstracción innecesaria. En un sistema TMS con queries complejos, joins múltiples y RLS personalizado, Dapper da más control sin penalización de performance significativa para CRUD standard.
2. **Firebase / Firestore** — Descartado por ser NoSQL document-oriented. Un TMS requiere relaciones transaccionales fuertes (órdenes ↔ embarques → pagos), lo cual es natural en PostgreSQL pero complejo en documentos desnormalizados.
3. **Neon / PlanetScale** — Descartados porque eliminan capacidades avanzadas de PostgreSQL (RLS policies, funciones PL/pgSQL, triggers personalizados) que son esenciales para el aislamiento multi-tenant.

## Consecuencias
**Positivas:**
- Control total sobre SQL optimizado para escenarios TMS complejos
- Sin overhead de ORM mapping ni query tracking
- Compatibilidad completa con características avanzadas de PostgreSQL
- Debugging transparente mediante logs SQL reales

**Negativas / Trade-offs:**
- Se pierde la productividad de migrations automáticas de EF Core (mitigable con Supabase CLI)
- Mayor responsabilidad manual en gestión de schemas
- Curva de aprendizaje ligeramente mayor para desarrolladores acostumbrados a ORMs completos

## Módulos Afectados
Todos. Este ADR define la tecnología base de toda la solución.

---
