// Controllers/ExportController.cs
namespace HackathonCoordinator.WebAPI.DTOs
{
    public class TeamMemberDto
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public bool IsCaptain { get; set; }
    }
}