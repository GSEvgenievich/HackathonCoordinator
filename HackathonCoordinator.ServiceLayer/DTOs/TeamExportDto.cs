namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class TeamExportDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TeamMemberDto> Members { get; set; }
        public List<TaskExportDto> Tasks { get; set; }
        public TeamStatsDto TeamStats { get; set; }
    }
}