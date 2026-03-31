namespace HackathonCoordinator.WebAPI.DTOs
{
    public class UserResultDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; }
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public int Place { get; set; }
        public string PlaceDisplay { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<FinalTeamMemberDto> FinalTeamMembers { get; set; } = new();
    }
}
