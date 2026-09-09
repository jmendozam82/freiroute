namespace Freiroute.Utility.Constants;

/// <summary>
/// Motivos válidos de rechazo de entrega (HU-030).
/// Corresponde a 'rechazos_entrega.motivo' VARCHAR(40).
/// </summary>
public static class TipoRechazo
{
    public const string ClienteAusente       = "CLIENTE_AUSENTE";
    public const string DireccionIncorrecta  = "DIRECCION_INCORRECTA";
    public const string MercanciaDanada      = "MERCANCIA_DANADA";
    public const string RechazoCliente       = "RECHAZO_CLIENTE";
    public const string Otro                 = "OTRO";

    /// <summary>Todos los motivos válidos — usado por validadores y vistas.</summary>
    public static readonly IReadOnlyList<string> Todos = new[]
    {
        ClienteAusente, DireccionIncorrecta, MercanciaDanada,
        RechazoCliente, Otro
    };
}