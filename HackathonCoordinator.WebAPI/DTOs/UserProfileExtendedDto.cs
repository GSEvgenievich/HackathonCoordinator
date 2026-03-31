namespace HackathonCoordinator.WebAPI.DTOs
{
    public class UserProfileExtendedDto : UserDto
    {
        public List<UserResultDto> Results { get; set; } = new();
        public bool IsCurrentUser { get; set; }

        public string RoleDisplay => RoleId switch
        {
            1 => "Администратор",
            2 => "Организатор",
            3 => "Капитан",
            _ => "Участник"
        };

        public string GitHubStatus => string.IsNullOrEmpty(GitHubUsername)
            ? "Не привязан"
            : $"Привязан ({GitHubUsername})";
    }
}
