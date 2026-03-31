using DocumentFormat.OpenXml.InkML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class UserProfileExtendedDto : UserDto
    {
        public List<UserResultDto> Results { get; set; } = new();
        public bool IsCurrentUser { get; set; }

        public string RoleDisplay => RoleId switch
        {
            1 => "Администратор",
            2 => "Организатор",
            3 => "Капитан",
            _ => "Участник"
        };

        public string GitHubStatus => string.IsNullOrEmpty(GitHubUsername)
            ? "Не привязан"
            : $"Привязан ({GitHubUsername})";
    }
}
