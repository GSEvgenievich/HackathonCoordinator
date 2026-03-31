namespace HackathonCoordinator.WebAPI.DTOs
{
    public class FinalTeamMemberDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Username { get; set; }
        public string PositionName { get; set; }
        public string RoleName { get; set; }
        public DateTime FixedAt { get; set; }
    }
}
