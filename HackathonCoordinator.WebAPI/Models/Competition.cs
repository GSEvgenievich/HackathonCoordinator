using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Competition
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int CreatedById { get; set; }

    public virtual User CreatedBy { get; set; } = null!;

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
