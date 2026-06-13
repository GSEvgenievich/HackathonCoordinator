using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Result
{
    public int Id { get; set; }

    public int CompetitionId { get; set; }

    public int TeamId { get; set; }

    public int Place { get; set; }

    public string PlaceDisplay { get; set; } = null!;

    public string? Comment { get; set; }

    public virtual Competition Competition { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;
}
