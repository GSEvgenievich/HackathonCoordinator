namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class SaveTeamResultDto
    {
        public int CompetitionId { get; set; }
        public int TeamId { get; set; }
        public int Place { get; set; }
        public string Comment { get; set; }
    }
}
