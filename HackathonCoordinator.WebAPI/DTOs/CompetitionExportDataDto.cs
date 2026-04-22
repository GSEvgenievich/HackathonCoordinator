// Controllers/ExportController.cs
namespace HackathonCoordinator.WebAPI.DTOs
{
    public class CompetitionExportDataDto
    {
        public CompetitionDto Competition { get; set; }
        public List<TeamExportDto> Teams { get; set; }
        public CompetitionStatsDto Stats { get; set; }
        public string SuggestedFileName { get; set; }
        public List<TeamResultDto> Results { get; set; } = new();
    }
}