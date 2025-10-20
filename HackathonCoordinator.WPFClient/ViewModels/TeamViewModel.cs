using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class TeamViewModel : INotifyPropertyChanged
    {
        private readonly TeamService _teamService;
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

        public ObservableCollection<MemberDto> Members { get; set; } = new();
        public ObservableCollection<ProjectDto> Projects { get; set; } = new();

        public RelayCommand OpenChatCommand { get; }
        public RelayCommand<int> OpenProjectCommand { get; }
        public RelayCommand<int> OpenProjectChatCommand { get; }
        public RelayCommand LeaveTeamCommand { get; }

        public TeamViewModel()
        {
            _teamService = new TeamService();
            _navigationService = App.NavigationService;

            OpenChatCommand = new RelayCommand(() =>
            {
                //_navigationService.NavigateTo(new TeamChatPage());
            });

            OpenProjectCommand = new RelayCommand<int>(id =>
            {
                MessageBox.Show($"Открываем проект {id}");
            });

            OpenProjectChatCommand = new RelayCommand<int>(id =>
            {
                MessageBox.Show($"Открываем чат проекта {id}");
            });

            LeaveTeamCommand = new RelayCommand(async () =>
            {
                var confirm = MessageBox.Show("Вы уверены, что хотите покинуть команду?",
                                              "Подтверждение", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes)
                    return;

                var leaveResult = await _teamService.LeaveTeamAsync();

                if (leaveResult.Success)
                {
                    MessageBox.Show("Вы покинули команду.");
                    _navigationService.NavigateTo(new NoTeamPage());
                }
                else
                {
                    MessageBox.Show(leaveResult.Message);
                }
            });

            LoadTeamDataAsync();
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

            Members.Clear();
            foreach (var m in team.Members)
                Members.Add(m);

            Projects.Clear();
            foreach (var p in team.Projects)
                Projects.Add(p);
        }
    }
}