using System.Text.Json;
using Confluent.Kafka;
using NotificationsService.Infrastructure.Kafka;
using NotificationsService.Src.Modules.Notifications.Services;

namespace NotificationsService.Infrastructure.Kafka;

public class NotificationEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<NotificationEventConsumer> _logger;

    public NotificationEventConsumer(
        KafkaSettings kafkaSettings,
        IServiceProvider serviceProvider,
        ILogger<NotificationEventConsumer> logger)
    {
        _kafkaSettings = kafkaSettings ?? throw new ArgumentNullException(nameof(kafkaSettings));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrEmpty(kafkaSettings.BootstrapServers))
        {
            throw new ArgumentException("Kafka BootstrapServers cannot be null or empty", nameof(kafkaSettings));
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000,
            MaxPollIntervalMs = 300000
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Reason}", e.Reason))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken); // Starting delay to ensure readiness
        
        _consumer.Subscribe(_kafkaSettings.Topics.NotificationEvents);
        
        _logger.LogInformation("Notification event consumer started... Listening to topic: {Topic}", 
            _kafkaSettings.Topics.NotificationEvents);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    
                    if (consumeResult?.Message != null)
                    {
                        await ProcessNotificationEvent(consumeResult.Message.Value);
                        _consumer.Commit(consumeResult);
                        
                        _logger.LogInformation("Processed message from offset {Offset}", 
                            consumeResult.Offset);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning("Kafka consume error: {Message}. Retrying after 5 seconds...", ex.Message);
                    await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification event");
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task ProcessNotificationEvent(string messageValue)
    {
        try
        {
            var notificationEvent = JsonSerializer.Deserialize<NotificationEvent>(messageValue);
            if (notificationEvent == null)
            {
                _logger.LogWarning("Failed to deserialize notification event");
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var kafkaProducer = scope.ServiceProvider.GetRequiredService<IKafkaProducer>();

            _logger.LogInformation("Processing notification event for notification {NotificationId}, attempt {Attempt}", 
                notificationEvent.NotificationId, notificationEvent.AttemptNumber);

            try
            {
                await notificationService.ProcessNotificationAsync(notificationEvent.NotificationId);
                _logger.LogInformation("Successfully processed notification {NotificationId}", 
                    notificationEvent.NotificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process notification {NotificationId}, attempt {Attempt}", 
                    notificationEvent.NotificationId, notificationEvent.AttemptNumber);

                if (notificationEvent.AttemptNumber < _kafkaSettings.RetryAttempts)
                {
                    notificationEvent.AttemptNumber++;
                    await Task.Delay(_kafkaSettings.RetryDelayMs * notificationEvent.AttemptNumber);
                    await kafkaProducer.ProduceAsync(_kafkaSettings.Topics.NotificationRetries, notificationEvent);
                    
                    _logger.LogInformation("Queued notification {NotificationId} for retry, attempt {Attempt}", 
                        notificationEvent.NotificationId, notificationEvent.AttemptNumber);
                }
                else
                {
                    await kafkaProducer.ProduceAsync(_kafkaSettings.Topics.NotificationDLQ, notificationEvent);
                    _logger.LogError("Notification {NotificationId} failed all retry attempts, sent to DLQ", 
                        notificationEvent.NotificationId);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize notification event: {Message}", messageValue);
        }
    }

    public override void Dispose()
    {
        try
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka consumer");
        }
        finally
        {
            base.Dispose();
        }
    }
}