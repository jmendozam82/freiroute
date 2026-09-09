namespace Freiroute.Utility.Constants;

/// <summary>
/// Valores por defecto de configuración operativa del tenant.
/// Sprint 5 (HU-030 CA-08): el cargo de re-entrega se calcula como
/// <c>tarifa.precio_unitario × factor_reentrega</c>.
///
/// <b>Decisión de arquitectura (Fase 1 Sprint 5):</b> la tabla
/// <c>configuracion</c> del tenant NO existe como tabla física — la
/// configuración operativa se persiste en la tabla <c>empresas</c>
/// (ver IConfiguracionRepository, "NO tiene tabla propia"). Tampoco
/// existe la columna <c>factor_reentrega</c> en ninguna tabla. Por eso
/// se define el default en código y @IngenieroDatos agrega la columna
/// <c>factor_reentrega NUMERIC(4,2) NOT NULL DEFAULT 1.5</c> a la tabla
/// <c>empresas</c> en la migración Sprint 5.
/// </summary>
public static class ConfiguracionDefaults
{
    /// <summary>Multiplicador por defecto del cargo de re-entrega (1.5× la tarifa original).</summary>
    public const decimal FactorReentregaDefault = 1.5m;
}