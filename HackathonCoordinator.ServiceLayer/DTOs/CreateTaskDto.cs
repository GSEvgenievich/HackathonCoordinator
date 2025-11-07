namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int TypeId { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? Deadline { get; set; }
        public string? GithubBranchName { get; set; }
    }
}
