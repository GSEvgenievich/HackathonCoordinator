using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Chat
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTime CreatedAt { get; set; }

    public int TypeId { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual ChatType Type { get; set; } = null!;
}
