using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.WebAPI.DTOs
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
