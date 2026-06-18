using Matrix_mizrachi.Models;

namespace Matrix_mizrachi.Services;

/// <summary>
/// No-op Kafka producer used when Kafka is disabled in configuration.
/// </summary>
public class NoOpKafkaProducerService : IKafkaProducerService
{
    private readonly ILogger<NoOpKafkaProducerService> _logger;

    public NoOpKafkaProducerService(ILogger<NoOpKafkaProducerService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task PublishAsync(MathOperationEvent mathEvent)
    {
        _logger.LogDebug("Kafka is disabled. Skipping publish for operation '{Operation}'.", mathEvent.Operation);
        return Task.CompletedTask;
    }
}
