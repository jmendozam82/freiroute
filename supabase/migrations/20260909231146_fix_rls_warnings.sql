-- ====================================================================
-- MIGRACIÓN: Fix Supabase Cloud Linter "RLS Policy Always True"
-- Objetivo: Resolver los 5 warnings por políticas demasiado permisivas.
-- ====================================================================

-- 1. codigos_2fa_temporales
-- Tabla estrictamente interna de seguridad. No debe ser expuesta a PostgREST.
-- Se elimina la política "ALL USING (true)". Al estar RLS habilitado, 
-- automáticamente deniega todo acceso a roles sin BYPASSRLS (ej: anon, authenticated).
-- El backend en C# (Dapper) se conecta como 'postgres' (superuser), por lo que 
-- ignora RLS y sigue funcionando con normalidad.
DROP POLICY IF EXISTS "codigos_2fa_allow_all" ON public.codigos_2fa_temporales;

-- 2. pagos
-- Tabla interna/administrativa.
DROP POLICY IF EXISTS "pagos_allow_all" ON public.pagos;

-- 3. suscripciones
-- Tabla administrativa.
DROP POLICY IF EXISTS "suscripciones_allow_all" ON public.suscripciones;

-- 4. empresas
-- Tabla raíz. El frontend puede necesitar leer datos básicos del tenant 
-- (logos, colores) en la pantalla de login antes de autenticarse.
-- Se reemplaza ALL por SELECT.
DROP POLICY IF EXISTS "empresas_allow_all" ON public.empresas;
CREATE POLICY "empresas_allow_select" ON public.empresas FOR SELECT USING (true);

-- 5. planes
-- Catálogo global de planes. El frontend de signup necesita leerlos.
-- Se reemplaza ALL por SELECT.
DROP POLICY IF EXISTS "planes_allow_all" ON public.planes;
CREATE POLICY "planes_allow_select" ON public.planes FOR SELECT USING (true);
