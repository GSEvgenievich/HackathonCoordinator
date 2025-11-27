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
        private readonly TaskService _taskService;
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string TeamName => CurrentTeam?.Name ?? "";
        public string GitHubRepoUrl => CurrentTeam?.GitHubUrl ?? "Не указан";

        private void UpdateGitProperties()
        {
            OnPropertyChanged(nameof(HasGitHubRepo));
            OnPropertyChanged(nameof(GitHubButtonText));
            OnPropertyChanged(nameof(IsGitHubButtonVisible));
            OnPropertyChanged(nameof(CanConnectGitHub));
        }

        public bool HasGitHubRepo => !string.IsNullOrEmpty(GitHubRepoUrl) && GitHubRepoUrl != "Не указан";
        public bool CanConnectGitHub => IsCaptain && !HasGitHubRepo;
        public string GitHubButtonText => HasGitHubRepo ? "🔗 Открыть" : "🔗 Подключить";
        public string InviteCode => CurrentTeam?.InviteCode ?? "";
        public int MembersCount => Members?.Count ?? 0;
        public int TotalTasksCount => TaskSections.Sum(s => s.Tasks.Count);
        public bool HasNoTasks => TotalTasksCount == 0;
        public int CompletedTasksCount => TaskSections.FirstOrDefault(s => s.StatusId == 4)?.Tasks.Count ?? 0;

        private TeamDto? _currentTeam;
        public TeamDto? CurrentTeam
        {
            get => _currentTeam;
            set { _currentTeam = value; OnPropertyChanged(); }
        }

        private bool _isCaptain;
        public bool IsCaptain
        {
            get => _isCaptain;
            set
            {
                _isCaptain = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCaptainOrOrganizer));
                OnPropertyChanged(nameof(IsGitHubButtonVisible));
            }
        }
        private bool _isOrganizer;
        public bool IsOrganizer
        {
            get => _isOrganizer;
            set
            {
                _isOrganizer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCaptainOrOrganizer));
            }
        }

        public bool IsCaptainOrOrganizer => IsCaptain || IsOrganizer;
        public bool IsTeamMember => !IsOrganizer;
        public bool IsGitHubButtonVisible => HasGitHubRepo || IsCaptain;

        private bool _showTransferDialog;
        private UserProfileDto? _currentUser;
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

        private bool _showCreateRepoDialog;
        public bool ShowCreateRepoDialog
        {
            get => _showCreateRepoDialog;
            set { _showCreateRepoDialog = value; OnPropertyChanged(); }
        }

        private string _newRepoName;
        public string NewRepoName
        {
            get => _newRepoName;
            set { _newRepoName = value; OnPropertyChanged(); }
        }

        private string _newRepoDescription;
        public string NewRepoDescription
        {
            get => _newRepoDescription;
            set { _newRepoDescription = value; OnPropertyChanged(); }
        }

        private bool _newRepoIsPrivate = true;
        public bool NewRepoIsPrivate
        {
            get => _newRepoIsPrivate;
            set { _newRepoIsPrivate = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MemberDto> Members { get; set; } = new();
        public ObservableCollection<TaskSection> TaskSections { get; set; } = new();
        public ObservableCollection<MemberDto> AvailableMembers { get; set; } = new();

        public ICommand LeaveTeamCommand { get; }
        public ICommand CreateTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand OpenTaskCommand { get; }
        public ICommand ToggleSectionCommand { get; }
        public ICommand CopyInviteCodeCommand { get; }
        public ICommand GitHubCommand { get; }
        public ICommand TransferLeadershipCommand { get; }
        public ICommand ConfirmTransferCommand { get; }
        public ICommand CancelTransferCommand { get; }
        public ICommand OpenTeamChatCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CreateGitHubRepoCommand { get; }
        public ICommand CancelCreateRepoCommand { get; }

        public TeamViewModel()
        {
            _teamService = new TeamService();
            _userService = new UserService();
            _taskService = new TaskService();
            _navigationService = App.NavigationService;

            LeaveTeamCommand = new RelayCommand(async () => await ExecuteLeaveTeamAsync());
            CreateTaskCommand = new RelayCommand(() => ExecuteCreateTask());
            OpenTeamChatCommand = new RelayCommand(OpenTeamChat);
            EditTaskCommand = new RelayCommand<TaskDto>(task => ExecuteEditTask(task));
            DeleteTaskCommand = new RelayCommand<TaskDto>(task => ExecuteDeleteTask(task));
            OpenTaskCommand = new RelayCommand<TaskDto>(task => ExecuteOpenTask(task));
            GitHubCommand = new RelayCommand(async () => await ExecuteGitHubCommandAsync());
            ToggleSectionCommand = new RelayCommand<int>(statusId => ToggleSection(statusId));
            CopyInviteCodeCommand = new RelayCommand(() => ExecuteCopyInviteCode());
            TransferLeadershipCommand = new RelayCommand(() => ExecuteTransferLeadership());
            ConfirmTransferCommand = new RelayCommand(async () => await ExecuteConfirmTransferAsync());
            CancelTransferCommand = new RelayCommand(() => ExecuteCancelTransfer());
            BackCommand = new RelayCommand(ExecuteBackCommand);
            CreateGitHubRepoCommand = new RelayCommand(async () => await ExecuteCreateGitHubRepoAsync());
            CancelCreateRepoCommand = new RelayCommand(ExecuteCancelCreateRepo);
        }

        private async void ExecuteBackCommand()
        {
            var competition = await _teamService.GetCompetitionByTeamIdAsync(CurrentTeam.Id);
            _navigationService.NavigateTo(new CompetitionDetailsPage(competition));
        }

        private async Task CreateGitHubRepositoryAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (string.IsNullOrEmpty(user?.GitHubUsername))
            {
                MessageBox.Show("Для создания репозитория необходимо сначала привязать GitHub аккаунт в профиле");
                _navigationService.NavigateTo(new ProfilePage());
                return;
            }

            NewRepoName = "NewRepository";
            NewRepoDescription = $"Проект команды {TeamName} для хакатона";
            ShowCreateRepoDialog = true;
        }

        private async Task ExecuteCreateGitHubRepoAsync()
        {
            if (string.IsNullOrWhiteSpace(NewRepoName))
            {
                ShowErrorMessage("Ошибка", "Введите название репозитория");
                return;
            }

            try
            {
                var result = await _teamService.CreateGitHubRepositoryAsync(
                    CurrentTeam.Id,
                    NewRepoName,
                    NewRepoDescription,
                    NewRepoIsPrivate);

                if (result.Success)
                {
                    ShowSuccessMessage("Репозиторий создан",
                        $"{result.Message}\n\nURL репозитория: {result.RepoUrl}");
                    ShowCreateRepoDialog = false;

                    await LoadTeamDataAsync(null);
                }
                else
                {
                    HandleGitHubError(result.Message, result.ErrorType);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка", $"Неожиданная ошибка: {ex.Message}");
            }
        }

        private void HandleGitHubError(string errorMessage, string errorType)
        {
            string title = "Ошибка создания репозитория";

            switch (errorType)
            {
                case "name_already_exists":
                    title = "Репозиторий уже существует";
                    break;
                case "auth_error":
                    title = "Ошибка авторизации";
                    break;
                case "network_error":
                    title = "Ошибка соединения";
                    break;
                case "rate_limit":
                    title = "Превышен лимит запросов";
                    break;
            }

            ShowErrorMessage(title, errorMessage);
        }

        private void ShowSuccessMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ExecuteCancelCreateRepo()
        {
            ShowCreateRepoDialog = false;
            NewRepoName = string.Empty;
            NewRepoDescription = string.Empty;
        }

        private async Task ExecuteLeaveTeamAsync()
        {
            var message = "Вы уверены, что хотите покинуть команду?";

            if (HasGitHubRepo)
            {
                message += "\n\n⚠️ Внимание: GitHub репозиторий команды будет отсоединен из-за ограничения доступа!";
            }

            if (MessageBox.Show(message, "Подтверждение выхода",
                                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var result = await _teamService.LeaveTeamAsync();
            if (result.Success)
            {
                MessageBox.Show("Вы покинули команду.");
                _navigationService.NavigateTo(new CompetitionsPage());
            }
            else
            {
                MessageBox.Show(result.Message);
            }
        }

        private async Task ExecuteGitHubCommandAsync()
        {
            if (HasGitHubRepo)
            {
                if (!string.IsNullOrEmpty(GitHubRepoUrl))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = GitHubRepoUrl,
                        UseShellExecute = true
                    });
                }
            }
            else
            {
                if (IsCaptain)
                {
                    await CreateGitHubRepositoryAsync();
                }
                else
                {
                    MessageBox.Show("Только капитан команды может подключать GitHub репозиторий");
                }
            }
        }

        private void OpenTeamChat()
        {
            if (CurrentTeam.ChatId != null)
            {
                MessageBox.Show($"Открытие чата команды (ID: {CurrentTeam.ChatId})");
                // Реализовать переход в чат
            }
        }

        private void ExecuteCreateTask()
        {
            if (CurrentTeam?.Id != null)
            {
                _navigationService.NavigateTo(new EditTaskPage(CurrentTeam.Id, true));
            }
        }

        private void ExecuteEditTask(TaskDto task)
        {
            if (task != null)
            {
                _navigationService.NavigateTo(new EditTaskPage(task.Id, false));
            }
        }

        private async void ExecuteDeleteTask(TaskDto task)
        {
            if (task == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить задачу \"{task.Title}\"?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var deleteResult = await _taskService.DeleteTaskAsync(task.Id);
                MessageBox.Show(deleteResult.Message);

                if (deleteResult.Success)
                {
                    // Перезагружаем задачи
                    await LoadTasksAsync();
                }
            }
        }

        private void ExecuteOpenTask(TaskDto task)
        {
            if (task != null)
            {
                _navigationService.NavigateTo(new TaskDetailsPage(task.Id));
            }
        }

        private void ToggleSection(int statusId)
        {
            var section = TaskSections.FirstOrDefault(s => s.StatusId == statusId);
            if (section != null)
            {
                section.IsExpanded = !section.IsExpanded;
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

            var message = $"Вы уверены, что хотите передать права капитана участнику {SelectedNewCaptain.Username}?";

            if (HasGitHubRepo)
            {
                message += "\n\n⚠️ Внимание: GitHub репозиторий команды будет отсоединен из-за ограничения доступа!";
            }

            if (MessageBox.Show(message, "Подтверждение передачи прав",
                              MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
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

                    await LoadTeamDataAsync(null);
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

        private void ExecuteCopyInviteCode()
        {
            Clipboard.SetText(InviteCode ?? "");
            MessageBox.Show("Код приглашения скопирован!");
        }

        public async Task LoadTeamDataAsync(int? teamId)
        {
            if (teamId == null)
            {
                CurrentTeam = await _teamService.GetCurrentTeamAsync();
            }
            else
            {
                CurrentTeam = await _teamService.GetTeamByIdAsync(teamId);
            }

            if (CurrentTeam == null)
            {
                MessageBox.Show("Команда не найдена");
                _navigationService.NavigateTo(new CompetitionsPage());
                return;
            }

            _currentUser = await _userService.GetCurrentUserAsync();

            await LoadMembersAsync();
            await LoadTasksAsync();
            await CheckUserRole();
            UpdateAllProperties();
        }

        private async Task LoadMembersAsync()
        {

            Members.Clear();
            foreach (var member in CurrentTeam.Members)
            {
                member.IsCurrentUser = member.Id == _currentUser?.Id;
                Members.Add(member);
            }

            OnPropertyChanged(nameof(MembersCount));
        }

        private async Task LoadTasksAsync()
        {
            if (CurrentTeam?.Id == null) return;

            // Используем задачи из CurrentTeam или загружаем отдельно
            var tasks = CurrentTeam.Tasks ?? await _teamService.GetTeamTasksAsync(CurrentTeam.Id);

            var taskSections = new List<TaskSection>
            {
                new TaskSection { StatusId = 1, StatusName = "В планах", Tasks = new ObservableCollection<TaskDto>(), IsExpanded = true },
                new TaskSection { StatusId = 2, StatusName = "В процессе", Tasks = new ObservableCollection<TaskDto>(), IsExpanded = true },
                new TaskSection { StatusId = 3, StatusName = "На проверке", Tasks = new ObservableCollection<TaskDto>(), IsExpanded = true },
                new TaskSection { StatusId = 4, StatusName = "Завершена", Tasks = new ObservableCollection<TaskDto>(), IsExpanded = false },
                new TaskSection { StatusId = 5, StatusName = "Отменена", Tasks = new ObservableCollection<TaskDto>(), IsExpanded = false }
            };

            foreach (var task in tasks)
            {
                task.IsMyTask = task.AssignedToId == _currentUser.Id;

                var section = taskSections.FirstOrDefault(s => s.StatusId == task.StatusId);
                section?.Tasks.Add(task);
            }

            TaskSections.Clear();
            foreach (var section in taskSections)
            {
                section.MyTasksCount = section.Tasks.Count(t => t.IsMyTask);
                TaskSections.Add(section);
            }

            OnPropertyChanged(nameof(TotalTasksCount));
            OnPropertyChanged(nameof(CompletedTasksCount));
            OnPropertyChanged(nameof(HasNoTasks));
        }

        private async Task CheckUserRole()
        {
            IsCaptain = _currentUser.RoleId == 1;
            IsOrganizer = _currentUser.RoleId == 3;

            OnPropertyChanged(nameof(IsCaptain));
            OnPropertyChanged(nameof(IsOrganizer));
            OnPropertyChanged(nameof(IsCaptainOrOrganizer));
            OnPropertyChanged(nameof(IsTeamMember));
        }

        private void UpdateDerivedProperties()
        {
            OnPropertyChanged(nameof(TeamName));
            OnPropertyChanged(nameof(InviteCode));
            OnPropertyChanged(nameof(GitHubRepoUrl));
            UpdateGitProperties();
        }

        private void UpdateAllProperties()
        {
            UpdateDerivedProperties();
            OnPropertyChanged(nameof(MembersCount));
            OnPropertyChanged(nameof(TotalTasksCount));
            OnPropertyChanged(nameof(CompletedTasksCount));
            OnPropertyChanged(nameof(HasNoTasks));
        }
    }

    public class TaskSection : INotifyPropertyChanged
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public ObservableCollection<TaskDto> Tasks { get; set; } = new();

        private int _myTasksCount;
        public int MyTasksCount
        {
            get => _myTasksCount;
            set
            {
                _myTasksCount = value;
                OnPropertyChanged();
            }
        }

        public bool HasMyTasks => MyTasksCount > 0;

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}