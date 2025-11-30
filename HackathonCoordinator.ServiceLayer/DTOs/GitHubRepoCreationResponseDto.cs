using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class GitHubRepoCreationResponseDto
    {
        public string Message { get; set; }
        public string RepoUrl { get; set; }
        public string RepoName { get; set; }
    }
}
