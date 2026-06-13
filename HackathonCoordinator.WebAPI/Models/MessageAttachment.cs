using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class MessageAttachment
{
    public int Id { get; set; }

    public int MessageId { get; set; }

    public string FileName { get; set; } = null!;

    public long FileSize { get; set; }

    public string ContentType { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public byte[]? Thumbnail { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual Message Message { get; set; } = null!;
}
