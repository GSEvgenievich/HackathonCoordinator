using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Task
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public int? AssignedToId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int TypeId { get; set; }

    public int StatusId { get; set; }

    public DateTime? Deadline { get; set; }

    public string? GithubBranchName { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ChatId { get; set; }

    public bool IsDeadlineNotified { get; set; }

    public bool IsDeadlineApproachNotified { get; set; }

    public virtual User? AssignedTo { get; set; }

    public virtual Chat Chat { get; set; } = null!;

    public virtual TaskStatus Status { get; set; } = null!;

    public virtual Team Team { get; set; } = null!;

    public virtual TaskType Type { get; set; } = null!;
}
