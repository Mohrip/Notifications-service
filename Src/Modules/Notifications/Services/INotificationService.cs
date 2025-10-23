using NotificationsService.Src.Modules.Notifications.DTO;

namespace NotificationsService.Src.Modules.Notifications.Services;

public interface INotificationService
{
    Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request);
    Task<NotificationResponse?> GetNotificationAsync(Guid id);
    Task ProcessNotificationAsync(Guid notificationId);
}