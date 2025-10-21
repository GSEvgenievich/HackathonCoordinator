using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class TeamViewModel : INotifyPropertyChanged
    {
        private readonly TeamService _teamService;
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _teamName;
        public string TeamName
        {
            get => _teamName;
            set { _teamName = value; OnPropertyChanged(); }
        }

        private string _inviteCode;
        public string InviteCode
        {
            get => _inviteCode;
            set { _inviteCode = value; OnPropertyChanged(); }
        }

        private bool _isCaptain;
        public bool IsCaptain
        {
            get => _isCaptain;
            set { _isCaptain = value; OnPropertyChanged(); }
        }

        public string ProjectsCountText => $"Проектов: {Projects?.Count ?? 0}";
        public bool HasProjects => Projects?.Any() == true;
        public ObservableCollection<MemberDto> Members { get; set; } = new();
        public ObservableCollection<ProjectDto> Projects { get; set; } = new();

        public ICommand LeaveTeamCommand { get; }
        public ICommand CreateProjectCommand { get; }
        public ICommand CopyInviteCodeCommand { get; }
        public ICommand SelectProjectCommand { get; }

        public TeamViewModel()
        {
            _teamService = new TeamService();
            _userService = new UserService();
            _navigationService = App.NavigationService;

            LeaveTeamCommand = new RelayCommand(async () =>
            {
                if (MessageBox.Show("Вы уверены, что хотите покинуть команду?", "Подтверждение",
                                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                var result = await _teamService.LeaveTeamAsync();
                if (result.Success)
                {
                    MessageBox.Show("Вы покинули команду.");
                    _navigationService.NavigateTo(new NoTeamPage());
                }
                else MessageBox.Show(result.Message);
            });

            SelectProjectCommand = new RelayCommand<ProjectDto>(OnSelectProject);

            CreateProjectCommand = new RelayCommand(() =>
            {
                MessageBox.Show("Открывается окно создания проекта (реализуй позже).");
            });

            CopyInviteCodeCommand = new RelayCommand(() =>
            {
                Clipboard.SetText(InviteCode ?? "");
                MessageBox.Show("Код приглашения скопирован!");
            });

            LoadTeamDataAsync();
        }
        private void OnSelectProject(ProjectDto project)
        {
            if (project == null) return;
            MessageBox.Show($"Проект выбран: {project.Name}");
        }

        private async void LoadTeamDataAsync()
        {
            var team = await _teamService.GetCurrentTeamAsync();
            if (team == null)
            {
                MessageBox.Show("Вы не состоите в команде.");
                _navigationService.NavigateTo(new NoTeamPage());
                return;
            }

            TeamName = team.Name;
            InviteCode = team.InviteCode;

            var user = await _userService.GetCurrentUserAsync();

            if (user.RoleId == 1)
                IsCaptain = true;

            var orderedMembers = team.Members
                .OrderByDescending(m => m.IsCaptain)
                .ThenBy(m => m.Username)
                .ToList();

            Members.Clear();
            foreach (var m in orderedMembers)
            {
                m.IsCurrentUser = m.Id == user.Id;
                Members.Add(m);
            }

            Projects.Clear();
            foreach (var p in team.Projects)
                Projects.Add(p);

            OnPropertyChanged(nameof(ProjectsCountText));
        }
    }
}