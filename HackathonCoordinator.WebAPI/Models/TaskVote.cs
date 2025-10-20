using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class TaskVote
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public bool? IsResultsVisible { get; set; }

    public bool? IsClosed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public virtual Task Task { get; set; } = null!;

    public virtual ICollection<TaskVoteResponse> TaskVoteResponses { get; set; } = new List<TaskVoteResponse>();
}
