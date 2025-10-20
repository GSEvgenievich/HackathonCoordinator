using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Document
{
    public int Id { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    public Guid FileKey { get; set; }

    public string FileName { get; set; } = null!;

    public long? FileSize { get; set; }

    public int CategoryId { get; set; }

    public int UploadedById { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual FileCategory Category { get; set; } = null!;

    public virtual Project? Project { get; set; }

    public virtual Task? Task { get; set; }

    public virtual User UploadedBy { get; set; } = null!;
}
