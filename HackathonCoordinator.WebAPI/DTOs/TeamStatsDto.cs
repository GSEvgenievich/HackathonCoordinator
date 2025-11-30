// Controllers/ExportController.cs
namespace HackathonCoordinator.WebAPI.DTOs
{
    public class TeamStatsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PlannedTasks { get; set; }
        public int CompletionPercentage { get; set; }
    }
}