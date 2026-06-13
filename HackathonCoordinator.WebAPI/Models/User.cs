using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public int RoleId { get; set; }

    public int PositionId { get; set; }

    public int? TeamId { get; set; }

    public int? ProfileIconId { get; set; }

    public string? GitHubUsername { get; set; }

    public string? GitHubAccessToken { get; set; }

    public string? GitHubAvatarUrl { get; set; }

    public virtual ICollection<Competition> CompetitionCreatedBies { get; set; } = new List<Competition>();

    public virtual ICollection<Competition> CompetitionResultsCreatedBies { get; set; } = new List<Competition>();

    public virtual ICollection<Competition> CompetitionResultsUpdatedBies { get; set; } = new List<Competition>();

    public virtual ICollection<FinalTeamMember> FinalTeamMembers { get; set; } = new List<FinalTeamMember>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Position Position { get; set; } = null!;

    public virtual ProfileIcon? ProfileIcon { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual Team? Team { get; set; }
}
