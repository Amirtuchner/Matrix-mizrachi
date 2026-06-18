using Matrix_mizrachi.Models;

namespace Matrix_mizrachi.Services;

/// <summary>
/// Publishes math operation events to Kafka.
/// </summary>
public interface IKafkaProducerService
{
    /// <summary>
    /// Publishes a math operation event to the math-operations Kafka topic.
    /// Retries up to 3 times with exponential back-off on failure.
    /// </summary>
    Task PublishAsync(MathOperationEvent mathEvent);
}
