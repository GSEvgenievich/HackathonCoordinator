using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class FileCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
