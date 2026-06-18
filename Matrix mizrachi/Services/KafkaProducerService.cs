using System.Text.Json;
using Confluent.Kafka;
using Matrix_mizrachi.Models;

namespace Matrix_mizrachi.Services;

/// <summary>
/// Publishes math operation events to Kafka using Confluent.Kafka.
/// </summary>
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private const string Topic = "math-operations";
    private const int MaxRetries = 3;

    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task PublishAsync(MathOperationEvent mathEvent)
    {
        var json = JsonSerializer.Serialize(mathEvent);

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await _producer.ProduceAsync(Topic, new Message<Null, string> { Value = json });
                _logger.LogInformation("Kafka event published for operation '{Operation}' (attempt {Attempt})", mathEvent.Operation, attempt);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka publish attempt {Attempt}/{MaxRetries} failed", attempt, MaxRetries);
                if (attempt < MaxRetries)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }

        _logger.LogError("Failed to publish Kafka event after {MaxRetries} attempts. Continuing.", MaxRetries);
    }

    public void Dispose() => _producer?.Dispose();
}
