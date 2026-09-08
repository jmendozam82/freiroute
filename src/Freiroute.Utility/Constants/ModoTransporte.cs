namespace Freiroute.Utility.Constants;

/// <summary>
/// Modos de transporte (tablas 'tarifas_base.modo_transporte',
/// 'embarques.modo_transporte' y 'empresas.modos_transporte_activos').
/// La clase no existía antes de Sprint 3 — OnboardingPaso3Validator tenía
/// los valores hardcodeados; @BackendDev puede refactorizarlo a
/// ModoTransporte.Todos en la Fase 3.
/// </summary>
public static class ModoTransporte
{
    public const string Terrestre = "TERRESTRE";  // Sprint 4: default en ordenes de transporte
    public const string Ftl = "FTL";
    public const string Ltl = "LTL";
    public const string Aereo = "AEREO";
    public const string Maritimo = "MARITIMO";
    public const string Ferroviario = "FERROVIARIO";
    public const string Intermodal = "INTERMODAL";

    /// <summary>Todos los modos válidos — usado por validadores y filtros.</summary>
    public static readonly string[] Todos =
    {
        Terrestre, Ftl, Ltl, Aereo, Maritimo, Ferroviario, Intermodal
    };
}