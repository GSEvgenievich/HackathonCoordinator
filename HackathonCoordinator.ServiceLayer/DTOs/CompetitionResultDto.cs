using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class CompetitionResultDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; }
        public List<TeamResultDto> Teams { get; set; } = new();
    }
}
