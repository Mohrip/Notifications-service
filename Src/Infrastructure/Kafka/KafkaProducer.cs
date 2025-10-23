using System.Text.Json;
using Confluent.Kafka;
using NotificationsService.Infrastructure.Kafka;

namespace NotificationsService.Infrastructure.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(string topic, T message);
    Task ProduceNotificationEventAsync(NotificationEvent notificationEvent);
}

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(KafkaSettings kafkaSettings, ILogger<KafkaProducer> logger)
    {
        _kafkaSettings = kafkaSettings ?? throw new ArgumentNullException(nameof(kafkaSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(kafkaSettings.BootstrapServers))
        {
            throw new ArgumentException("Kafka BootstrapServers cannot be null or empty", nameof(kafkaSettings));
        }

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            Acks = Acks.All, // This is Required when EnableIdempotence = true to ensure message durability (usually set to All in production)
            RetryBackoffMs = 1000,
            MessageSendMaxRetries = 3,
            RequestTimeoutMs = 30000,
            MessageTimeoutMs = 300000,
            EnableIdempotence = true,
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Reason}", e.Reason))
            .Build();
    }

    public async Task ProduceAsync<T>(string topic, T message)
    {
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
        }

        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage);
            
            _logger.LogInformation("Message produced to topic {Topic},  {Partition},  {Offset}", 
                result.Topic, result.Partition, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic}: {Error}", topic, ex.Error?.Reason);
            throw new InvalidOperationException($"Failed to produce message to topic {topic}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize message for topic {Topic}", topic);
            throw new InvalidOperationException($"Failed to serialize message for topic {topic}", ex);
        }
    }

    public async Task ProduceNotificationEventAsync(NotificationEvent notificationEvent)
    {
        await ProduceAsync(_kafkaSettings.Topics.NotificationEvents, notificationEvent);
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}