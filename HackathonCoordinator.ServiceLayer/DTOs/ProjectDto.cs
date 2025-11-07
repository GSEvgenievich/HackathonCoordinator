namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class ProjectDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string GithubRepoName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ChatId { get; set; }
        public string TeamName { get; set; }
    }
}
