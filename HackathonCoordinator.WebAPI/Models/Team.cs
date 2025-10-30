using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Team
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }

    public string Name { get; set; } = null!;

    public string? InviteCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? GitHubUrl { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual Competition Competition { get; set; } = null!;

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
