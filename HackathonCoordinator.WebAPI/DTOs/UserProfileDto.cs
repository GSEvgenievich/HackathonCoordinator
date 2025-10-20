namespace HackathonCoordinator.WebAPI.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? TeamName { get; set; }
        public int? IconId { get; set; }
        public string? IconName { get; set; }
    }
}
