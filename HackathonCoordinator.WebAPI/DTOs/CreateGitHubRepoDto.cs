namespace HackathonCoordinator.WebAPI.DTOs
{
    public class CreateGitHubRepoDto
    {
        public string RepoName { get; set; }
        public string Description { get; set; }
        public bool IsPrivate { get; set; } = true;
    }
}
