using HackathonCoordinator.WebAPI.Services;

namespace HackathonCoordinator.WebAPI.DTOs
{
    public class GitHubAuthResultDto
    {
        public string AccessToken { get; set; }
        public GitHubUserInfo UserInfo { get; set; }
    }
}