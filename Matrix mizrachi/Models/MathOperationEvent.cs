namespace Matrix_mizrachi.Models;

/// <summary>
/// Kafka event published on every cache miss.
/// </summary>
public class MathOperationEvent
{
    /// <summary>Unique identifier for this request.</summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Arithmetic operation type.</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>First operand.</summary>
    public double X { get; set; }

    /// <summary>Second operand.</summary>
    public double Y { get; set; }

    /// <summary>Result of the operation.</summary>
    public double Result { get; set; }

    /// <summary>UTC timestamp of the event.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
