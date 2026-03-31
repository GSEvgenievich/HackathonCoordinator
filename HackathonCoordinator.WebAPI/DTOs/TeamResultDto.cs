using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.WebAPI.DTOs
{
    public class TeamResultDto
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public int? Place { get; set; }
        public string PlaceDisplay { get; set; }
        public string Comment { get; set; }
        public string Icon { get; set; } = "🏆";
        public bool IsSaved { get; set; }
        public string MembersCount { get; set; }
    }
}
