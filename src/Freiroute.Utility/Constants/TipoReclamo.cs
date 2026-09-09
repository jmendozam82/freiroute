namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de reclamo formal por incidencias de entrega (HU-032).
/// Corresponde a 'reclamos.tipo' VARCHAR(20).
/// </summary>
public static class TipoReclamo
{
    public const string Dano    = "DANO";
    public const string Perdida = "PERDIDA";
    public const string Retraso = "RETRASO";
    public const string Otro    = "OTRO";

    /// <summary>Todos los tipos válidos — usado por validadores y filtros.</summary>
    public static readonly IReadOnlyList<string> Todos =
        new[] { Dano, Perdida, Retraso, Otro };
}