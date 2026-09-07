namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de unidad de medida (tabla 'unidades_medida.tipo', HU-018).
/// Incluye las unidades base por tipo del sistema.
/// </summary>
public static class TipoMedida
{
    public const string Peso = "PESO";
    public const string Volumen = "VOLUMEN";
    public const string Longitud = "LONGITUD";
    public const string Temperatura = "TEMPERATURA";

    // ── Unidades base por tipo ─────────────────────────────────
    public const string BaseKg = "kg";
    public const string BaseM3 = "m3";
    public const string BaseM = "m";
    public const string BaseC = "C";
}