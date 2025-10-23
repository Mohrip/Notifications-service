namespace NotificationsService.Src.Shared.Enums;

public enum NotificationChannel
{
    Email = 0,
    SMS = 1,
    Push = 2

    // Future channels can be added here then modify the NotificationService accordingly in he switch case
}