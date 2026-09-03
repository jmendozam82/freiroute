# ADR-001: Stack Tecnológico Freiroute TMS

## Estado
Aceptado

## Fecha
2026

## Contexto
Freiroute TMS es un sistema SaaS multi-tenant de gestión de transporte de nivel mundial. Necesita una arquitectura que soporte aislamiento de datos por empresa, autenticación segura con JWT, escalabilidad horizontal, y desarrollo ágil con el equipo actual que tiene experiencia en .NET y PostgreSQL.

## Decisión
El sistema usará **ASP.NET Core (.NET 8) + Supabase (PostgreSQL 15) + Dapper** como stack principal.

## Alternativas Consideradas

1. **Node.js + Prisma + PlanetScale** — Descartada porque el equipo no tiene experiencia en Node y Prisma no ofrece control SQL fino para RLS complejo.
2. **Django + PostgreSQL** — Descartada porque requiere reaprender el stack completo y el ORM de Django dificulta la implementación de RLS a nivel de sesión.
3. **ASP.NET Core + Entity Framework Core** — Descartada porque EF Core abstrae demasiado el SQL y dificulta el control granular de `empresa_id` en queries complejas de TMS. Dapper da control total.

## Consecuencias

**Positivas:**
- El equipo conoce C# y .NET — curva de aprendizaje mínima
- Supabase provee Auth, RLS, Storage y Realtime listos para usar
- Dapper permite SQL explícito — crítico para queries multi-join de TMS
- PostgreSQL 15 soporta RLS nativo — aislamiento multi-tenant a nivel de BD
- Supabase CLI simplifica migraciones versionadas

**Negativas / Trade-offs:**
- Supabase tiene vendor lock-in parcial (Auth, Storage, Realtime)
- Dapper requiere escribir SQL manualmente — más código que EF Core
- .NET en Linux (contenedores) requiere configuración adicional vs. Windows

## Módulos Afectados
Todos los módulos del sistema (EP-01 al EP-20).
