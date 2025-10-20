using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.WebAPI.DTOs
{
    public class TeamDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string InviteCode { get; set; }
        public List<MemberDto> Members { get; set; } = new();
        public List<ProjectDto> Projects { get; set; } = new();
    }
}
