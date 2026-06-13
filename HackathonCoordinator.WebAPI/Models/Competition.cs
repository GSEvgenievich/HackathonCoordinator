using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Competition
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedById { get; set; }

    public bool IsArchived { get; set; }

    public bool HasResults { get; set; }

    public bool IsStartNotified { get; set; }

    public bool IsEndNotified { get; set; }

    public DateTime? ResultsCreatedAt { get; set; }

    public int? ResultsCreatedById { get; set; }

    public DateTime? ResultsUpdatedAt { get; set; }

    public int? ResultsUpdatedById { get; set; }

    public virtual User CreatedBy { get; set; } = null!;

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    public virtual User? ResultsCreatedBy { get; set; }

    public virtual User? ResultsUpdatedBy { get; set; }

    public virtual ICollection<Stage> Stages { get; set; } = new List<Stage>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
