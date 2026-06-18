namespace Matrix_mizrachi.Services;

/// <summary>
/// Performs arithmetic calculations.
/// </summary>
public interface IMathService
{
    /// <summary>
    /// Calculates the result of an arithmetic operation on two numbers.
    /// </summary>
    /// <param name="operation">Operation name: add, subtract, multiply, divide.</param>
    /// <param name="x">First operand.</param>
    /// <param name="y">Second operand.</param>
    /// <returns>The result of the operation.</returns>
    double Calculate(string operation, double x, double y);
}
