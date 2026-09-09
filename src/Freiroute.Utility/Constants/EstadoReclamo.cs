namespace Freiroute.Utility.Constants;

/// <summary>
/// Máquina de estados de un reclamo (HU-032).
/// Corresponde a 'reclamos.estado' VARCHAR(20) DEFAULT 'ABIERTO'.
/// Transiciones: ABIERTO → EN_REVISION → APROBADO/RECHAZADO → CERRADO.
/// </summary>
public static class EstadoReclamo
{
    public const string Abierto    = "ABIERTO";
    public const string EnRevision = "EN_REVISION";
    public const string Aprobado   = "APROBADO";
    public const string Rechazado  = "RECHAZADO";
    public const string Cerrado    = "CERRADO";

    /// <summary>
    /// Transiciones válidas de la FSM de reclamos — la BLL valida contra
    /// este mapa antes de persistir (CA-03 / CA-12 de HU-032).
    /// </summary>
    public static readonly Dictionary<string, string[]> Transiciones = new()
    {
        [Abierto]    = new[] { EnRevision },
        [EnRevision] = new[] { Aprobado, Rechazado },
        [Aprobado]   = new[] { Cerrado },
        [Rechazado]  = new[] { Cerrado },
        [Cerrado]    = Array.Empty<string>()
    };
}