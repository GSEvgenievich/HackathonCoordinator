namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class TaskExportDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string AssignedTo { get; set; }
        public DateTime? Deadline { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}