using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NotificationsService.Src.Modules.Notifications.DTO;
using NotificationsService.Src.Modules.Notifications.Models;
using NotificationsService.Src.Modules.Notifications.Repository;
using NotificationsService.Src.Shared.Enums;
using NotificationsService.Infrastructure.Kafka;

namespace NotificationsService.Src.Modules.Notifications.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;
    private readonly IKafkaProducer? _kafkaProducer;

    public NotificationService(
        INotificationRepository repository,
        ILogger<NotificationService> logger,
        IKafkaProducer? kafkaProducer = null)
    {
        _repository = repository;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

/* In this class I added IdempotencyKey handling to prevent duplicate notifications,
and I added logging for better observability with input validated requests */
    public async Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Channel))
        {
            throw new ArgumentException("Channel cannot be null or empty", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Template))
        {
            throw new ArgumentException("Template cannot be null or empty", nameof(request));
        }

        if (!Enum.TryParse<NotificationChannel>(request.Channel, true, out var channel))
        {
            throw new ArgumentException($"Invalid channel: {request.Channel}");
        }

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existing = await _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existing != null)
            {
                _logger.LogInformation("Returning existing notification for idempotency key: {IdempotencyKey}", request.IdempotencyKey);
                return MapToResponse(existing);
            }
        }

        var notification = new Notification
        {
            UserId = request.UserId,
            Channel = channel,
            Template = request.Template,
            Data = JsonSerializer.Serialize(request.Data),
            IdempotencyKey = request.IdempotencyKey
        };

        try
        {
            await _repository.CreateAsync(notification);
            _logger.LogInformation("Notification {NotificationId} created for user {UserId}", notification.Id, notification.UserId);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogInformation("Idempotency key collision detected for key: {IdempotencyKey}. Fetching existing notification.", request.IdempotencyKey);
            var existingNotification = await _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey!);
            if (existingNotification != null)
            {
                return MapToResponse(existingNotification);
            }

            throw;
        }

        // Use Kafka if it is available, if not process synchronously
        if (_kafkaProducer != null)
        {
            var notificationEvent = new NotificationEvent
            {
                NotificationId = notification.Id,
                EventType = "NotificationCreated",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            };

            await _kafkaProducer.ProduceNotificationEventAsync(notificationEvent);
            _logger.LogInformation("Notification {NotificationId} queued for async processing", notification.Id);
        }
        else
        {
            //  synchronous processing
            await ProcessNotificationAsync(notification.Id);
            var updatedNotification = await _repository.GetByIdAsync(notification.Id);
            return MapToResponse(updatedNotification!);
        }

        return MapToResponse(notification);
    }

    public async Task<NotificationResponse?> GetNotificationAsync(Guid id)
    {
        var notification = await _repository.GetByIdAsync(id);
        return notification != null ? MapToResponse(notification) : null;
    }


    public async Task ProcessNotificationAsync(Guid notificationId)
    {
        var notification = await _repository.GetByIdAsync(notificationId);
        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found", notificationId);
            return;
        }

        try
        {
            await ProcessByChannel(notification);
            
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            
            _logger.LogInformation("Notification {NotificationId} sent successfully via {Channel}", 
                notificationId, notification.Channel);
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
            notification.RetryCount++;
            
            _logger.LogError(ex, "Failed to send notification {NotificationId}", notificationId);
        }

        await _repository.UpdateAsync(notification);
    }

    private async Task ProcessByChannel(Notification notification)
    {
        await Task.Delay(100);

        var data = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Data);
        
        switch (notification.Channel)
        {
            case NotificationChannel.Email:
                _logger.LogInformation("Sending email to user {UserId} with template {Template}. Data: {Data}", 
                    notification.UserId, notification.Template, notification.Data);
                break;
            case NotificationChannel.SMS:
                _logger.LogInformation("Sending SMS to user {UserId} with template {Template}. Data: {Data}", 
                    notification.UserId, notification.Template, notification.Data);
                break;
            case NotificationChannel.Push:
                _logger.LogInformation("Sending push notification to user {UserId} with template {Template}. Data: {Data}", 
                    notification.UserId, notification.Template, notification.Data);
                break;
            default:
                throw new NotSupportedException($"Channel {notification.Channel} is not supported");
        }
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Channel = notification.Channel.ToString(),
            Template = notification.Template,
            Status = notification.Status.ToString(),
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt,
            ErrorMessage = notification.ErrorMessage,
            RetryCount = notification.RetryCount
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message?.Contains("UNIQUE constraint failed") == true ||
               ex.InnerException?.Message?.Contains("IX_Notifications_IdempotencyKey") == true;
    }
}