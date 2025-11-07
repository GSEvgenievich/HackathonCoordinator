using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class ChatType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();
}
