using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class FinalTeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public int? UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PositionName { get; set; } = null!;

    public int RoleId { get; set; }

    public DateTime FixedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;

    public virtual User? User { get; set; }
}
