using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int NotificationTypeId { get; set; }
    public string Title { get; set; } = null!; 
    public string Message { get; set; } = null!;
    public string? RelatedEntityType { get; set; } 
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public virtual NotificationType NotificationType { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
