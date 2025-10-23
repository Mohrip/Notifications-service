using Microsoft.EntityFrameworkCore;
using NotificationsService.Src.Modules.Notifications.Models;

namespace NotificationsService.Src.Modules.Notifications.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            // I added IdempotencyKey unique constraint to prevent duplicate notifications
            entity.HasIndex(e => e.IdempotencyKey)
                  .IsUnique()
                  .HasFilter("[IdempotencyKey] IS NOT NULL");
            
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.Property(e => e.Data).IsRequired();
        });
    }
}