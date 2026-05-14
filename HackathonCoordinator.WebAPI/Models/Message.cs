using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class Message
{
    public int Id { get; set; }

    public int ChatId { get; set; }

    public int UserId { get; set; }

    public string Text { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public bool IsEdited { get; set; }

    public DateTime? EditedAt { get; set; }

    public bool HasAttachments { get; set; }

    public virtual Chat Chat { get; set; } = null!;

    public virtual ICollection<MessageAttachment> MessageAttachments { get; set; } = new List<MessageAttachment>();

    public virtual User User { get; set; } = null!;
}
