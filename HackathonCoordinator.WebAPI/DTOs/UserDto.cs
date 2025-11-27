namespace HackathonCoordinator.WebAPI.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int RoleId { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string? GitHubUsername { get; set; }
        public string? GitHubAccessToken { get; set; }
        public int? IconId { get; set; }
        public string? IconName { get; set; }
    }
}
