namespace HackathonCoordinator.WebAPI.DTOs
{
    public class CreateTeamDto
    {
        public string Name { get; set; } = string.Empty;
        public bool LinkToGitHub { get; set; }
    }
}
