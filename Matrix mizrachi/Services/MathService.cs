namespace Matrix_mizrachi.Services;

/// <summary>
/// Performs arithmetic calculations.
/// </summary>
public class MathService : IMathService
{
    /// <inheritdoc/>
    public double Calculate(string operation, double x, double y)
    {
        return operation.ToLowerInvariant() switch
        {
            "add"      => x + y,
            "subtract" => x - y,
            "multiply" => x * y,
            "divide"   => y == 0
                            ? throw new DivideByZeroException("Cannot divide by zero.")
                            : x / y,
            _ => throw new ArgumentException($"Unknown operation: '{operation}'. Supported: add, subtract, multiply, divide.")
        };
    }
}
