using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Project
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? GithubRepoUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual Team Team { get; set; } = null!;
}
