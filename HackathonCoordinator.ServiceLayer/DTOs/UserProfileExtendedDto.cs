using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class UserProfileExtendedDto : UserDto, INotifyPropertyChanged
    {
        private string? _gitHubUsername;
        private string? _gitHubStatus;

        public List<UserResultDto> Results { get; set; } = new();
        public bool IsCurrentUser { get; set; }

        public string RoleDisplay => RoleId switch
        {
            1 => "Администратор",
            2 => "Организатор",
            3 => "Капитан",
            _ => "Участник"
        };

        public new string? GitHubUsername
        {
            get => _gitHubUsername;
            set
            {
                if (_gitHubUsername != value)
                {
                    _gitHubUsername = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(GitHubStatus));
                }
            }
        }

        public string GitHubStatus
        {
            get => string.IsNullOrEmpty(GitHubUsername) ? "Не привязан" : $"Привязан ({GitHubUsername})";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}