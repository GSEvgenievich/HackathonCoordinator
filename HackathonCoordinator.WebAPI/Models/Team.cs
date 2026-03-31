using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Team
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }

    public string Name { get; set; } = null!;

    public string InviteCode { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? GitRepoName { get; set; }

    public int ChatId { get; set; }

    public virtual Chat Chat { get; set; } = null!;

    public virtual Competition Competition { get; set; } = null!;

    public virtual ICollection<FinalTeamMember> FinalTeamMembers { get; set; } = new List<FinalTeamMember>();

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
