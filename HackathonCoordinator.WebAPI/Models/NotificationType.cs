using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class NotificationType
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Icon { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
