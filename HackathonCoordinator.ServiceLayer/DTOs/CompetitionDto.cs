using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class CompetitionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int CreatedById { get; set; }
        public string CreatedByUsername { get; set; }
        public List<TeamDto> Teams { get; set; } = new();
    }
}
