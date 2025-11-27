namespace HackathonCoordinator.WebAPI.DTOs
{
    public class TaskDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public int? AssignedToId { get; set; }
        public string? AssignedToUsername { get; set; }
        public DateTime? Deadline { get; set; }
        public string? GitHubBranchName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
