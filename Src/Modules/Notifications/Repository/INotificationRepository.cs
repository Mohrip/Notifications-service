using NotificationsService.Src.Modules.Notifications.Models;

namespace NotificationsService.Src.Modules.Notifications.Repository;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<Notification?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<IEnumerable<Notification>> GetAllAsync();
    Task UpdateAsync(Notification notification);
}