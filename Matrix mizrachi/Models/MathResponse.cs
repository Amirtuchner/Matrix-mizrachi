namespace Matrix_mizrachi.Models;

/// <summary>
/// Response from the math operation endpoint.
/// </summary>
public class MathResponse
{
    /// <summary>
    /// Result of the arithmetic operation.
    /// </summary>
    public double Result { get; set; }

    /// <summary>
    /// Description of the operation retrieved from Mockoon.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether the result was served from cache.
    /// </summary>
    public bool FromCache { get; set; }
}
