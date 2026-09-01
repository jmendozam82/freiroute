namespace Freiroute.Utility.ApiResponse;

/// <summary>
/// Envoltorio estandarizado para todas las respuestas de la API.
/// Cumple regla AGENTS.md: "Todas las respuestas usarán ApiResponse&lt;T&gt;".
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Respuesta exitosa estándar.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") =>
        new() { Success = true, Data = data, Message = message };

    /// <summary>
    /// Respuesta con error.
    /// </summary>
    public static ApiResponse<T> Error(string message, List<string>? details = null) =>
        new() { Success = false, Message = message, Errors = details };
}
