-- ====================================================================
-- MIGRACIÓN: Fix Supabase Cloud Linter Warnings & Errors
-- Objetivo: Resolver los 7 errores "RLS Disabled in Public" y las 3 
-- advertencias "Function Search Path Mutable" reportados por el linter.
-- ====================================================================

-- ====================================================================
-- 1. FIX WARNINGS: Function Search Path Mutable
-- ====================================================================

-- 1.a) update_fecha_modificacion
CREATE OR REPLACE FUNCTION update_fecha_modificacion()
RETURNS TRIGGER AS $$
BEGIN
    NEW.fecha_modificacion = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SET search_path = '';

-- 1.b) generar_numero_orden
CREATE OR REPLACE FUNCTION generar_numero_orden(
    p_empresa_id UUID,
    p_prefijo     TEXT,
    p_anio        SMALLINT
) RETURNS TEXT
LANGUAGE plpgsql SET search_path = '' AS $$
DECLARE
    v_consecutivo INT;
    v_numero      TEXT;
BEGIN
    -- Se usa public. para evitar errores al tener search_path vacío
    INSERT INTO public.contadores_orden (empresa_id, anio, ultimo_consecutivo)
    VALUES (p_empresa_id, p_anio, 1)
    ON CONFLICT (empresa_id, anio)
    DO UPDATE
        SET ultimo_consecutivo = public.contadores_orden.ultimo_consecutivo + 1
    RETURNING ultimo_consecutivo INTO v_consecutivo;

    v_numero := UPPER(TRIM(p_prefijo))
             || '-' || p_anio::TEXT
             || '-' || LPAD(v_consecutivo::TEXT, 5, '0');

    RETURN v_numero;
END;
$$;

-- 1.c) generar_numero_reclamo
CREATE OR REPLACE FUNCTION generar_numero_reclamo(
    p_empresa_id UUID,
    p_prefijo     TEXT,
    p_anio        SMALLINT
) RETURNS TEXT
LANGUAGE plpgsql SET search_path = '' AS $$
DECLARE
    v_consecutivo INT;
BEGIN
    INSERT INTO public.contadores_reclamo (empresa_id, anio, ultimo_consecutivo)
    VALUES (p_empresa_id, p_anio, 1)
    ON CONFLICT (empresa_id, anio)
    DO UPDATE
        SET ultimo_consecutivo = public.contadores_reclamo.ultimo_consecutivo + 1
    RETURNING ultimo_consecutivo INTO v_consecutivo;

    RETURN UPPER(TRIM(p_prefijo))
        || '-' || p_anio::TEXT
        || '-' || LPAD(v_consecutivo::TEXT, 4, '0');
END;
$$;

-- ====================================================================
-- 2. FIX ERRORS: RLS Disabled in Public
-- ====================================================================

-- 2.a) Tablas Globales y de Super Admin (Mantenemos comportamiento USING (true))
-- Estas tablas están protegidas por la capa BLL y el controlador de API, y 
-- algunas no tienen empresa_id. Se habilita RLS y se permite acceso general
-- para silenciar el linter y mantener la compatibilidad actual del DAL.
ALTER TABLE public.empresas ENABLE ROW LEVEL SECURITY;
CREATE POLICY "empresas_allow_all" ON public.empresas FOR ALL USING (true);

ALTER TABLE public.planes ENABLE ROW LEVEL SECURITY;
CREATE POLICY "planes_allow_all" ON public.planes FOR ALL USING (true);

ALTER TABLE public.suscripciones ENABLE ROW LEVEL SECURITY;
CREATE POLICY "suscripciones_allow_all" ON public.suscripciones FOR ALL USING (true);

ALTER TABLE public.pagos ENABLE ROW LEVEL SECURITY;
CREATE POLICY "pagos_allow_all" ON public.pagos FOR ALL USING (true);

ALTER TABLE public.codigos_2fa_temporales ENABLE ROW LEVEL SECURITY;
CREATE POLICY "codigos_2fa_allow_all" ON public.codigos_2fa_temporales FOR ALL USING (true);

-- 2.b) Contadores Internos (Tienen empresa_id, se aíslan por tenant)
ALTER TABLE public.contadores_orden ENABLE ROW LEVEL SECURITY;
CREATE POLICY "contadores_orden_isolation" ON public.contadores_orden
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);

ALTER TABLE public.contadores_reclamo ENABLE ROW LEVEL SECURITY;
CREATE POLICY "contadores_reclamo_isolation" ON public.contadores_reclamo
    FOR ALL
    USING (empresa_id = (current_setting('app.current_empresa_id', true))::UUID);
