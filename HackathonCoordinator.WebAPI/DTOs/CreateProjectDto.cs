using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.WebAPI.DTOs
{
    public class CreateProjectDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool CreateGitHubRepo { get; set; }
        public string GitHubRepoName { get; set; }
    }
}
