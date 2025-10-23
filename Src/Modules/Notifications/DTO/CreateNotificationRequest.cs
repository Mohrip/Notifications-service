using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace NotificationsService.Src.Modules.Notifications.DTO;

public class CreateNotificationRequest
{
    [Required(ErrorMessage = "User ID is required")]
    [StringLength(100, ErrorMessage = "User ID cannot exceed 100 characters")]
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Channel is required")]
    [StringLength(20, ErrorMessage = "Channel cannot exceed 20 characters")]
    public string Channel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Template is required")]
    [StringLength(100, ErrorMessage = "Template cannot exceed 100 characters")]
    public string Template { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data is required")]
    public Dictionary<string, object> Data { get; set; } = new();

    [StringLength(100, ErrorMessage = "Idempotency key cannot exceed 100 characters")]
    public string? IdempotencyKey { get; set; }
}

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}