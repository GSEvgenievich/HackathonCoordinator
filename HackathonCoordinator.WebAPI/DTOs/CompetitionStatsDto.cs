// Controllers/ExportController.cs
namespace HackathonCoordinator.WebAPI.DTOs
{
    public class CompetitionStatsDto
    {
        public int TotalParticipants { get; set; }
        public int TotalTasks { get; set; }
        public int TotalCompletedTasks { get; set; }
        public int TotalCompletionPercentage { get; set; }
        public int AverageTeamProgress { get; set; }
    }
}