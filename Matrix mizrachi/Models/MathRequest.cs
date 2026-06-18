namespace Matrix_mizrachi.Models;

/// <summary>
/// Request body for the math operation endpoint.
/// </summary>
public class MathRequest
{
    /// <summary>
    /// Arithmetic operation type (add, subtract, multiply, divide).
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// First operand.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Second operand.
    /// </summary>
    public double Y { get; set; }
}
