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
using System.Windows.Media;

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

        private TeamDto? _currentTeam;
        public TeamDto? CurrentTeam
        {
            get => _currentTeam;
            set { _currentTeam = value; OnPropertyChanged(); }
        }

        private string _teamGitHubUrl;
        public string TeamGitHubUrl
        {
            get => _teamGitHubUrl;
            set { _teamGitHubUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(TeamGitHubStatus)); }
        }

        private bool _isCaptain;
        public bool IsCaptain
        {
            get => _isCaptain;
            set { _isCaptain = value; OnPropertyChanged(); }
        }

        private bool _isCaptainOrOrganizer;
        public bool IsCaptainOrOrganizer
        {
            get => _isCaptainOrOrganizer;
            set { _isCaptainOrOrganizer = value; OnPropertyChanged(); }
        }

        private bool _isTeamMember;
        public bool IsTeamMember
        {
            get => _isTeamMember;
            set { _isTeamMember = value; OnPropertyChanged(); }
        }
        private bool _showTransferDialog;
        public bool ShowTransferDialog
        {
            get => _showTransferDialog;
            set { _showTransferDialog = value; OnPropertyChanged(); }
        }



        private MemberDto _selectedNewCaptain;
        public MemberDto SelectedNewCaptain
        {
            get => _selectedNewCaptain;
            set { _selectedNewCaptain = value; OnPropertyChanged(); }
        }

        public string TeamGitHubStatus => string.IsNullOrEmpty(TeamGitHubUrl)
            ? "Не привязан"
            : "Привязан";

        public Brush TeamGitHubStatusColor => string.IsNullOrEmpty(TeamGitHubUrl)
            ? Brushes.Red
            : Brushes.Green;

        public bool HasTeamGitHub => !string.IsNullOrEmpty(TeamGitHubUrl);

        public string ProjectsCountText => $"Проектов: {Projects?.Count ?? 0}";
        public bool HasProjects => Projects?.Any() == true;

        public ObservableCollection<MemberDto> Members { get; set; } = new();
        public ObservableCollection<ProjectDto> Projects { get; set; } = new();
        public ObservableCollection<MemberDto> AvailableMembers { get; set; } = new();

        public ICommand LeaveTeamCommand { get; }
        public ICommand CreateProjectCommand { get; }
        public ICommand CopyInviteCodeCommand { get; }
        public ICommand SelectProjectCommand { get; }
        public ICommand OpenTeamGitHubCommand { get; }
        public ICommand TransferLeadershipCommand { get; }
        public ICommand ConfirmTransferCommand { get; }
        public ICommand CancelTransferCommand { get; }

        public TeamViewModel()
        {
            _teamService = new TeamService();
            _userService = new UserService();
            _navigationService = App.NavigationService;

            LeaveTeamCommand = new RelayCommand(async () => await ExecuteLeaveTeamAsync());
            SelectProjectCommand = new RelayCommand<ProjectDto>(OnSelectProject);
            CreateProjectCommand = new RelayCommand(async () => await ExecuteCreateProjectAsync());
            CopyInviteCodeCommand = new RelayCommand(() => ExecuteCopyInviteCode());
            OpenTeamGitHubCommand = new RelayCommand(() => ExecuteOpenTeamGitHub());
            TransferLeadershipCommand = new RelayCommand(() => ExecuteTransferLeadership());
            ConfirmTransferCommand = new RelayCommand(async () => await ExecuteConfirmTransferAsync());
            CancelTransferCommand = new RelayCommand(() => ExecuteCancelTransfer());
        }

        private async Task ExecuteLeaveTeamAsync()
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
            else
            {
                MessageBox.Show(result.Message);
            }
        }

        private void ExecuteTransferLeadership()
        {
            // Фильтруем участников, исключая текущего капитана
            AvailableMembers.Clear();
            foreach (var member in Members.Where(m => !m.IsCaptain))
            {
                AvailableMembers.Add(member);
            }

            if (!AvailableMembers.Any())
            {
                MessageBox.Show("В команде нет других участников для передачи прав.");
                return;
            }

            SelectedNewCaptain = AvailableMembers.FirstOrDefault();
            ShowTransferDialog = true;
        }

        private async Task ExecuteConfirmTransferAsync()
        {
            if (SelectedNewCaptain == null)
            {
                MessageBox.Show("Выберите участника для передачи прав.");
                return;
            }

            if (MessageBox.Show($"Вы уверены, что хотите передать права капитана участнику {SelectedNewCaptain.Username}?",
                              "Подтверждение передачи прав",
                              MessageBoxButton.YesNo,
                              MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var result = await _teamService.AssignCaptainAsync(CurrentTeam.Id, SelectedNewCaptain.Id);

                if (result.Success)
                {
                    MessageBox.Show($"Права капитана успешно переданы участнику {SelectedNewCaptain.Username}");
                    ShowTransferDialog = false;

                    // Обновляем данные команды
                    await LoadTeamDataAsync(CurrentTeam.Id);
                }
                else
                {
                    MessageBox.Show(result.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при передаче прав: {ex.Message}");
            }
        }

        private void ExecuteCancelTransfer()
        {
            ShowTransferDialog = false;
            SelectedNewCaptain = null;
        }

        private void OnSelectProject(ProjectDto project)
        {
            if (project == null) return;
            
            _navigationService.NavigateTo(new ProjectPage(project));
        }

        private async Task ExecuteCreateProjectAsync()
        {
            var teamId = await _teamService.GetCurrentTeamIdAsync();

            if (teamId != null)
            {
                _navigationService.NavigateTo(new EditProjectPage(teamId.Value));
            }
            else
            {
                MessageBox.Show("Не удалось определить команду");
            }
        }

        private void ExecuteCopyInviteCode()
        {
            Clipboard.SetText(InviteCode ?? "");
            MessageBox.Show("Код приглашения скопирован!");
        }

        private void ExecuteOpenTeamGitHub()
        {
            if (!string.IsNullOrEmpty(TeamGitHubUrl))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = TeamGitHubUrl,
                    UseShellExecute = true
                });
            }
        }

        public async Task LoadTeamDataAsync(int? teamId)
        {
            if (teamId == null)
            {
                CurrentTeam = await _teamService.GetCurrentTeamAsync();
                if (CurrentTeam == null)
                {
                    MessageBox.Show("Вы не состоите в команде.");
                    _navigationService.NavigateTo(new CompetitionsPage());
                    return;
                }
            }
            else
            {
                CurrentTeam = await _teamService.GetTeamByIdAsync(teamId);
                if (CurrentTeam == null)
                {
                    MessageBox.Show("Команда не найдена");
                    _navigationService.NavigateTo(new CompetitionsPage());
                    return;
                }
            }

            TeamName = CurrentTeam.Name;
            InviteCode = CurrentTeam.InviteCode;
            TeamGitHubUrl = CurrentTeam.GitHubUrl;

            var user = await _userService.GetCurrentUserAsync();

            // Обновляем статус капитана
            IsCaptain = user?.RoleId == 1;
            IsCaptainOrOrganizer = user?.RoleId == 3 || IsCaptain;
            IsTeamMember = user?.RoleId == 2 || IsCaptain;

            // Сортируем участников: капитан первый, затем остальные
            var orderedMembers = CurrentTeam.Members
                .OrderByDescending(m => m.IsCaptain)
                .ThenBy(m => m.Username)
                .ToList();

            Members.Clear();
            foreach (var m in orderedMembers)
            {
                m.IsCurrentUser = m.Id == user?.Id;
                Members.Add(m);
            }

            Projects.Clear();
            foreach (var p in CurrentTeam.Projects)
                Projects.Add(p);

            // Обновляем все свойства
            OnPropertyChanged(nameof(ProjectsCountText));
            OnPropertyChanged(nameof(TeamGitHubStatus));
            OnPropertyChanged(nameof(TeamGitHubStatusColor));
            OnPropertyChanged(nameof(HasTeamGitHub));
            OnPropertyChanged(nameof(IsCaptain));
        }
    }
}