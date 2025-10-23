using NotificationsService.Src.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace NotificationsService.Src.Modules.Notifications.Models;

public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public NotificationChannel Channel { get; set; }
    
    [Required]
    public string Template { get; set; } = string.Empty;
    
    [Required]
    public string Data { get; set; } = string.Empty; 
    
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? SentAt { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public string? IdempotencyKey { get; set; }
}