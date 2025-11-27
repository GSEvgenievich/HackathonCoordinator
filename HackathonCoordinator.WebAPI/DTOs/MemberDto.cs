namespace HackathonCoordinator.WebAPI.DTOs
{
    public class MemberDto
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string RoleName { get; set; }
        public string? IconName { get; set; }
        public bool IsCaptain { get; set; }
        public string IconPath => $"Assets/Images/Profile/{IconName ?? "robot1"}.png";
    }
}
