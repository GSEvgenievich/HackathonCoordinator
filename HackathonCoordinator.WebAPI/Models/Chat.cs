using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Chat
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public int? TypeId { get; set; }

    public virtual ICollection<ChatMember> ChatMembers { get; set; } = new List<ChatMember>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual ChatType? Type { get; set; }
}
