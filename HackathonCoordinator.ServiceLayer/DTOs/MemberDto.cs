namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class MemberDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }
        public string IconName { get; set; }
        public bool IsCurrentUser { get; set; }
        public bool IsCaptain => RoleName == "Капитан";
        public string IconPath => $"/Assets/Images/Profile/{IconName ?? "robot1"}.png";
    }
}
