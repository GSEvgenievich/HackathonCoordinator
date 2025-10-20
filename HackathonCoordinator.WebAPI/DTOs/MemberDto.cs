using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.WebAPI.DTOs
{
    public class MemberDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string RoleName { get; set; }
        public string IconName { get; set; }

        public string IconPath => $"Assets/Images/Profile/{IconName ?? "robot1"}.png";
    }
}
