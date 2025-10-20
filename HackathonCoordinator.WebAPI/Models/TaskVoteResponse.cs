using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class TaskVoteResponse
{
    public int Id { get; set; }

    public int TaskVoteId { get; set; }

    public int UserId { get; set; }

    public int EstimatedHours { get; set; }

    public virtual TaskVote TaskVote { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
