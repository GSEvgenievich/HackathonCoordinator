namespace HackathonCoordinator.WebAPI.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int TypeId { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? Deadline { get; set; }
        public string? GitHubBranchName { get; set; }
    }
}
