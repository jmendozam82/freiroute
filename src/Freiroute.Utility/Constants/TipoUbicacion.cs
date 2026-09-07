namespace Freiroute.Utility.Constants;

/// <summary>
/// Tipos de ubicación del catálogo de maestros (tabla 'ubicaciones.tipo').
/// Usado en HU-015 y en la geocodificación para clasificar puntos.
/// </summary>
public static class TipoUbicacion
{
    public const string Almacen = "ALMACEN";
    public const string Cliente = "CLIENTE";
    public const string Puerto = "PUERTO";
    public const string Aeropuerto = "AEROPUERTO";
    public const string Terminal = "TERMINAL";
    public const string CruceFrontera = "CRUCE_FRONTERA";
    public const string PuntoRecarga = "PUNTO_RECARGA";
    public const string Otro = "OTRO";

    /// <summary>Todos los tipos válidos — usado por validadores y filtros.</summary>
    public static readonly string[] Todos =
    {
        Almacen, Cliente, Puerto, Aeropuerto,
        Terminal, CruceFrontera, PuntoRecarga, Otro
    };
}