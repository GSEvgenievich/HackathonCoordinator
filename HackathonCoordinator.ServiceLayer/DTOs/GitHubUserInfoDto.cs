using Newtonsoft.Json;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class GitHubUserInfoDto
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }
    }
}