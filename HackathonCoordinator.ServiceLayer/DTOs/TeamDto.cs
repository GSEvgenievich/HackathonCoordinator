namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ChatId { get; set; }
        public string InviteCode { get; set; }
        public string? GitHubUrl { get; set; }
        public List<MemberDto> Members { get; set; } = new();
        public List<TaskDto> Tasks { get; set; } = new();
    }
}
