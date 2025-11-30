namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class GitHubAuthResultDto
    {
        public string AccessToken { get; set; }
        public GitHubUserInfoDto UserInfo { get; set; }
    }
}