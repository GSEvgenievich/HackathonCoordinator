namespace HackathonCoordinator.WebAPI.DTOs
{
    public class StageDto
    {
        public int Id { get; set; }
        public int CompetitionId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public int Order { get; set; }
        public bool IsFinal { get; set; }
        public bool IsStartNotified { get; set; }
    }

    public class StageSaveDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public int Order { get; set; }
        public bool IsFinal { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
