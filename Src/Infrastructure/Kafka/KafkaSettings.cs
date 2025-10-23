namespace NotificationsService.Infrastructure.Kafka;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public KafkaTopics Topics { get; set; } = new();
    public string GroupId { get; set; } = "notification-service";
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

public class KafkaTopics
{
    public string NotificationEvents { get; set; } = "notification-events";
    public string NotificationRetries { get; set; } = "notification-retries";
    public string NotificationDLQ { get; set; } = "notification-dlq";
}

public class NotificationEvent
{
    public Guid NotificationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int AttemptNumber { get; set; } = 1;
    public string? CorrelationId { get; set; }
}