namespace Freiroute.Utility.ApiResponse;

/// <summary>
/// Patrón de respuesta estándar de la API (ADR-008).
/// Cumple regla AGENTS.md: "Todas las respuestas usarán ApiResponse&lt;T&gt;".
/// Factory methods: Ok() para éxito y Fail() para error.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Respuesta exitosa estándar.</summary>
    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") =>
        new() { Success = true, Data = data, Message = message };

    /// <summary>Respuesta de error con detalles opcionales (método canónico).</summary>
    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };

    /// <summary>
    /// Alias de Fail() definido en ADR-008. Mantenido por compatibilidad de contrato.
    /// Preferir Fail() en código nuevo.
    /// </summary>
    public static ApiResponse<T> Error(string message, List<string>? details = null) =>
        Fail(message, details);
}