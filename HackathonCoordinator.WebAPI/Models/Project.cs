using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Project
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? GithubRepoName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? ChatId { get; set; }

    public virtual Chat? Chat { get; set; }

    public virtual ICollection<File> Files { get; set; } = new List<File>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();

    public virtual Team Team { get; set; } = null!;
}
