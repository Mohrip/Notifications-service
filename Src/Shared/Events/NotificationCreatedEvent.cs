using System;
using System.Collections.Generic;

namespace NotificationsService.Src.Shared.Events;

public record NotificationCreatedEvent(
    Guid NotificationId,
    string UserId,
    string Channel,
    string Template,
    Dictionary<string, object> Data,
    DateTime CreatedAt
);