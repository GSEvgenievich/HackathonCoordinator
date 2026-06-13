using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HackathonCoordinator.ServiceLayer.DTOs
{
    public class UserDto: INotifyPropertyChanged
    {
        private int? _iconId;
        private string? _iconName;
        private string _username = null!;

        public int Id { get; set; }
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }
        public string Email { get; set; } = null!;
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int PositionId { get; set; }
        public string PositionName { get; set; }
        public int? TeamId { get; set; }
        public string? TeamName { get; set; }
        public string? GitHubUsername { get; set; }
        public string? GitHubAccessToken { get; set; }
        public int? IconId
        {
            get => _iconId;
            set
            {
                _iconId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IconPath));
            }
        }

        public string? IconName
        {
            get => _iconName;
            set
            {
                _iconName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IconPath));
            }
        }
        public string IconPath => $"/Assets/Images/Profile/{IconName ?? "boy1"}.png";
        public string GitHubStatus => string.IsNullOrEmpty(GitHubUsername) ? "Не привязан" : "Привязан";
        public string TeamStatus => TeamId.HasValue ? "В команде" : "Свободен";
        public string TeamInfo => TeamId.HasValue ? $"Команда: {TeamName}" : "Не в команде";
        public bool HasTeam => TeamId.HasValue;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
