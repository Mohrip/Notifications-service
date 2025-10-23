using Microsoft.EntityFrameworkCore;
using NotificationsService.Src.Modules.Notifications.Data;
using NotificationsService.Src.Modules.Notifications.Models;

namespace NotificationsService.Src.Modules.Notifications.Repository;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task<Notification?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.IdempotencyKey == idempotencyKey);
    }

    public async Task<IEnumerable<Notification>> GetAllAsync()
    {
        return await _context.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }
}