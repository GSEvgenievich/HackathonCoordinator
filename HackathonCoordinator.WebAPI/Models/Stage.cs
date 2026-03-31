using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Stage
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string? Location { get; set; }

    public int Order { get; set; }

    public bool IsFinal { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Competition Competition { get; set; } = null!;
}
