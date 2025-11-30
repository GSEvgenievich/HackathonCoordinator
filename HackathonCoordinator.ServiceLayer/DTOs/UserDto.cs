namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string? GitHubUsername { get; set; }
        public string? GitHubAccessToken { get; set; }
        public int? IconId { get; set; }
        public string? IconName { get; set; }
        public string IconPath => $"/Assets/Images/Profile/{IconName ?? "boy1"}.png";
        public string GitHubStatus => string.IsNullOrEmpty(GitHubUsername) ? "Не привязан" : "Привязан";
        public string TeamStatus => TeamId.HasValue ? "В команде" : "Свободен";
        public string TeamInfo => TeamId.HasValue ? $"Команда: {TeamName}" : "Не в команде";
        public bool HasTeam => TeamId.HasValue;
    }
}
