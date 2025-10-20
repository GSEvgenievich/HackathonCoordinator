using System;
using System.Collections.Generic;

namespace HackathonCoordinator.WebAPI.Models;

public partial class PendingRegistration
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string VerificationCode { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool? IsVerified { get; set; }
}
