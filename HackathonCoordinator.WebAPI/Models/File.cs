using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class File
{
    public int Id { get; set; }

    public int? ProjectId { get; set; }

    public Guid FileKey { get; set; }

    public string FileName { get; set; } = null!;

    public long? FileSize { get; set; }

    public int CategoryId { get; set; }

    public int UploadedById { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual FileCategory Category { get; set; } = null!;

    public virtual Project? Project { get; set; }

    public virtual User UploadedBy { get; set; } = null!;
}
