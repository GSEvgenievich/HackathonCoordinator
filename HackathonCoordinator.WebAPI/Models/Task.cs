using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Task
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int? AssignedToId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int TypeId { get; set; }

    public int StatusId { get; set; }

    public DateTime? Deadline { get; set; }

    public string? GithubBranchName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? AssignedTo { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual Project Project { get; set; } = null!;

    public virtual TaskStatus Status { get; set; } = null!;

    public virtual TaskVote? TaskVote { get; set; }

    public virtual TaskType Type { get; set; } = null!;
}
