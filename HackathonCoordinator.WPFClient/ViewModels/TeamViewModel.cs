using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class TeamViewModel : BaseViewModel
    {
        public bool doDispose = true;
        private bool _isInitialized = false;

        private readonly TeamService _teamService;
        private readonly CompetitionService _competitionService;
        private readonly TaskService _taskService;
        private readonly ChatService _chatService;
        private readonly UserService _userService;

        private TeamDto _currentTeam;
        public TeamDto CurrentTeam
        {
            get => _currentTeam;
            set => SetProperty(ref _currentTeam, value);
        }

        public string TeamName => CurrentTeam?.Name ?? "";
        public string GitHubRepoUrl => CurrentTeam?.GitHubUrl ?? "Не указан";
        public bool HasGitHubRepo => !string.IsNullOrEmpty(GitHubRepoUrl) && GitHubRepoUrl != "Не указан";
        public bool CanConnectGitHub => IsCaptain && !HasGitHubRepo;
        public string GitHubButtonText => HasGitHubRepo ? "🔗 Открыть" : "🔗 Подключить";
        public string InviteCode => CurrentTeam?.InviteCode ?? "";
        public int MembersCount => Members?.Count ?? 0;
        public int TotalTasksCount => TaskSections.Sum(s => s.Tasks.Count);
        public bool HasNoTasks => TotalTasksCount == 0;
        public int CompletedTasksCount => TaskSections.FirstOrDefault(s => s.StatusId == 4)?.Tasks.Count ?? 0;

        private bool _canGoBack;
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        private bool _isCaptain;
        public bool IsCaptain
        {
            get => _isCaptain;
            set
            {
                SetProperty(ref _isCaptain, value);
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
                SetProperty(ref _isOrganizer, value);
                OnPropertyChanged(nameof(IsCaptainOrOrganizer));
            }
        }

        public bool IsCaptainOrOrganizer => IsCaptain || IsOrganizer;
        public bool IsTeamMember => !IsOrganizer;
        public bool IsGitHubButtonVisible => HasGitHubRepo || IsCaptain;

        private bool _showTransferDialog;
        private UserDto _currentUser;
        public bool ShowTransferDialog
        {
            get => _showTransferDialog;
            set => SetProperty(ref _showTransferDialog, value);
        }

        private MemberDto _selectedNewCaptain;
        public MemberDto SelectedNewCaptain
        {
            get => _selectedNewCaptain;
            set => SetProperty(ref _selectedNewCaptain, value);
        }

        private bool _showCreateRepoDialog;
        public bool ShowCreateRepoDialog
        {
            get => _showCreateRepoDialog;
            set => SetProperty(ref _showCreateRepoDialog, value);
        }

        private string _newRepoName;
        public string NewRepoName
        {
            get => _newRepoName;
            set => SetProperty(ref _newRepoName, value);
        }

        private string _newRepoDescription;
        public string NewRepoDescription
        {
            get => _newRepoDescription;
            set => SetProperty(ref _newRepoDescription, value);
        }

        private bool _newRepoIsPrivate = true;
        public bool NewRepoIsPrivate
        {
            get => _newRepoIsPrivate;
            set => SetProperty(ref _newRepoIsPrivate, value);
        }

        private bool _isArchived;
        public bool IsArchived
        {
            get => _isArchived;
            set
            {
                SetProperty(ref _isArchived, value);
                OnPropertyChanged(nameof(PageSubtitle));
                OnPropertyChanged(nameof(MembersSectionTitle));
            }
        }

        private string _competitionStatusText;
        public string CompetitionStatusText
        {
            get => _competitionStatusText;
            set => SetProperty(ref _competitionStatusText, value);
        }

        private string _competitionStatusColor;
        public string CompetitionStatusColor
        {
            get => _competitionStatusColor;
            set => SetProperty(ref _competitionStatusColor, value);
        }

        private bool _hasResult;
        public bool HasResult
        {
            get => _hasResult;
            set => SetProperty(ref _hasResult, value);
        }

        private int? _place;
        public int? Place
        {
            get => _place;
            set
            {
                SetProperty(ref _place, value);
                OnPropertyChanged(nameof(PlaceDisplay));
                OnPropertyChanged(nameof(PlaceBrush));
            }
        }

        public string PlaceDisplay => !Place.HasValue ? "—" : Place.Value switch
        {
            1 => "🥇 1 место",
            2 => "🥈 2 место",
            3 => "🥉 3 место",
            _ => $"{Place.Value} место"
        };

        public Brush PlaceBrush => Place switch
        {
            1 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),
            2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)),
            3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)),
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
        };

        private string _resultComment;
        public string ResultComment
        {
            get => _resultComment;
            set => SetProperty(ref _resultComment, value);
        }

        public string PageSubtitle => IsArchived ? "Просмотр архива команды" : "Управление проектом и задачами команды";
        public string MembersSectionTitle => IsArchived ? "👥 Финальный состав команды" : "👥 Участники команды";

        private ObservableCollection<FinalTeamMemberDto> _finalTeamMembers = new();
        public ObservableCollection<FinalTeamMemberDto> FinalTeamMembers
        {
            get => _finalTeamMembers;
            set => SetProperty(ref _finalTeamMembers, value);
        }

        public ObservableCollection<MemberDto> Members { get; set; } = new();
        public ObservableCollection<TaskSection> TaskSections { get; set; } = new();
        public ObservableCollection<MemberDto> AvailableMembers { get; set; } = new();

        // Команды
        public ICommand CreateTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ToggleSectionCommand { get; }
        public ICommand CopyInviteCodeCommand { get; }
        public ICommand GitHubCommand { get; }
        public ICommand TransferLeadershipCommand { get; }
        public ICommand ConfirmTransferCommand { get; }
        public ICommand CancelTransferCommand { get; }
        public ICommand OpenTeamChatCommand { get; }
        public ICommand CreateGitHubRepoCommand { get; }
        public ICommand CancelCreateRepoCommand { get; }
        public ICommand KickMemberCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand ViewMemberProfileCommand { get; }
        public ICommand LeaveTeamCommand { get; }
        public ICommand OpenTaskCommand { get; }

        public TeamViewModel()
        {
            _teamService = new TeamService();
            _competitionService = new CompetitionService();
            _userService = new UserService();
            _chatService = new ChatService();
            _taskService = new TaskService();

            CreateTaskCommand = new RelayCommand(ExecuteCreateTask,
                () => CurrentTeam?.Id != null && (IsCaptainOrOrganizer || CurrentTeam?.Tasks.Count < 10));

            DeleteTaskCommand = new AsyncRelayCommand<TaskDto>(ExecuteDeleteTaskAsync,
                task => task != null && IsCaptainOrOrganizer);

            OpenTaskCommand = new AsyncRelayCommand<TaskDto>(ExecuteOpenTaskAsync,
                task => task != null);

            ToggleSectionCommand = new RelayCommand<int>(ToggleSection);
            CopyInviteCodeCommand = new RelayCommand(ExecuteCopyInviteCode, () => !string.IsNullOrEmpty(InviteCode));
            TransferLeadershipCommand = new AsyncRelayCommand(ExecuteTransferLeadership, () => IsCaptain || (IsOrganizer && Members.Count > 0));
            CancelTransferCommand = new RelayCommand(ExecuteCancelTransfer);
            CancelCreateRepoCommand = new RelayCommand(ExecuteCancelCreateRepo);
            BackCommand = new RelayCommand(ExecuteBackCommand, () => CurrentTeam != null);
            EditTaskCommand = new AsyncRelayCommand<TaskDto>(ExecuteEditTask,
                task => task != null && (IsCaptainOrOrganizer || task.AssignedToId == _currentUser?.Id));
            LeaveTeamCommand = new AsyncRelayCommand(ExecuteLeaveTeamAsync, () => CurrentTeam != null);
            GitHubCommand = new AsyncRelayCommand(ExecuteGitHubCommandAsync, () => CurrentTeam != null);
            OpenTeamChatCommand = new AsyncRelayCommand(OpenTeamChatAsync, () => CurrentTeam?.ChatId != null);
            ConfirmTransferCommand = new AsyncRelayCommand(ExecuteConfirmTransferAsync, () => SelectedNewCaptain != null && CurrentTeam != null);
            CreateGitHubRepoCommand = new AsyncRelayCommand(ExecuteCreateGitHubRepoAsync, () => CurrentTeam != null && !string.IsNullOrWhiteSpace(NewRepoName));
            KickMemberCommand = new AsyncRelayCommand<MemberDto>(ExecuteKickMemberAsync, member => member != null && !member.IsCurrentUser && !member.IsCaptain);
            ViewMemberProfileCommand = new AsyncRelayCommand<MemberDto>(ExecuteViewMemberProfileAsync, member => member != null);
        }

        public async Task InitializeAsync(TeamDto team)
        {
            CanGoBack = _navigationService.CanGoBack;

            if (_isInitialized && CurrentTeam?.Id == team.Id) return;

            IsLoading = true;

            try
            {
                CurrentTeam = team;
                await LoadTeamDataAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки команды: {ex.Message}");
                await Application.Current.Dispatcher.InvokeAsync(() => Back());
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshTeamDataAsync(int? teamId)
        {
            try
            {
                var teamResponse = teamId == null
                    ? await _teamService.GetCurrentTeamAsync()
                    : await _teamService.GetTeamByIdAsync(teamId.Value);

                if (!teamResponse.Success || teamResponse.Data == null)
                {
                    await ShowErrorAsync("Команда не найдена");
                    await Application.Current.Dispatcher.InvokeAsync(() => Back());
                    return;
                }

                CurrentTeam = teamResponse.Data;
                await LoadTeamDataAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки команды: {ex.Message}");
                await Application.Current.Dispatcher.InvokeAsync(() => Back());
            }
        }

        private async Task LoadTeamDataAsync()
        {
            var competition = await _competitionService.GetCompetitionAsync(CurrentTeam.CompetitionId);
            if (competition.Success)
            {
                IsArchived = competition.Data.IsArchived;
                CompetitionStatusText = competition.Data.StatusText;
                CompetitionStatusColor = competition.Data.StatusColor;
                HasResult = competition.Data.HasResults;
            }

            var userResponse = await _userService.GetCurrentUserAsync();
            if (userResponse.Success)
            {
                _currentUser = userResponse.Data;
            }

            if (IsArchived)
            {
                await LoadFinalTeamDataAsync();
            }
            else
            {
                await LoadMembersAsync();
                await LoadTasksAsync();
            }

            if (HasResult)
            {
                await LoadTeamResultAsync();
            }

            CheckUserRole();
            UpdateAllProperties();
        }

        private async Task LoadFinalTeamDataAsync()
        {
            try
            {
                var finalMembers = await _teamService.GetFinalTeamMembersAsync(CurrentTeam.Id);
                if (finalMembers.Success && finalMembers.Data.Any())
                {
                    FinalTeamMembers = new ObservableCollection<FinalTeamMemberDto>(finalMembers.Data);

                    Members.Clear();
                    foreach (var member in finalMembers.Data)
                    {
                        Members.Add(new MemberDto
                        {
                            Id = member.UserId ?? 0,
                            Username = member.Username,
                            PositionName = member.PositionName,
                            RoleName = member.RoleName,
                            IsCurrentUser = member.UserId == _currentUser?.Id
                        });
                    }
                    OnPropertyChanged(nameof(MembersCount));
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки финального состава: {ex.Message}");
            }
        }

        private async Task LoadTeamResultAsync()
        {
            try
            {
                var result = await _teamService.GetTeamResultAsync(CurrentTeam.CompetitionId, CurrentTeam.Id);
                if (result.Success && result.Data != null)
                {
                    Place = result.Data.Place;
                    ResultComment = result.Data.Comment;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки результата: {ex.Message}");
            }
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

            var tasks = CurrentTeam.Tasks;

            var taskSections = new List<TaskSection>
            {
                new TaskSection { StatusId = 1, StatusName = "В планах", IsExpanded = false },
                new TaskSection { StatusId = 2, StatusName = "В процессе", IsExpanded = false },
                new TaskSection { StatusId = 3, StatusName = "На проверке", IsExpanded = false },
                new TaskSection { StatusId = 4, StatusName = "Завершена", IsExpanded = false },
                new TaskSection { StatusId = 5, StatusName = "Отменена", IsExpanded = false }
            };

            foreach (var task in tasks)
            {
                task.IsMyTask = task.AssignedToId == _currentUser?.Id;
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

        private void CheckUserRole()
        {
            IsCaptain = _currentUser?.RoleId == (int)Roles.Captain;
            IsOrganizer = _currentUser?.RoleId == (int)Roles.Organizer || _currentUser?.RoleId == (int)Roles.Admin;
            OnPropertyChanged(nameof(IsCaptain));
            OnPropertyChanged(nameof(IsOrganizer));
            OnPropertyChanged(nameof(IsCaptainOrOrganizer));
            OnPropertyChanged(nameof(IsTeamMember));
        }

        private void UpdateAllProperties()
        {
            OnPropertyChanged(nameof(TeamName));
            OnPropertyChanged(nameof(InviteCode));
            OnPropertyChanged(nameof(GitHubRepoUrl));
            OnPropertyChanged(nameof(HasGitHubRepo));
            OnPropertyChanged(nameof(CanConnectGitHub));
            OnPropertyChanged(nameof(GitHubButtonText));
            OnPropertyChanged(nameof(IsGitHubButtonVisible));
            OnPropertyChanged(nameof(MembersCount));
            OnPropertyChanged(nameof(TotalTasksCount));
            OnPropertyChanged(nameof(CompletedTasksCount));
            OnPropertyChanged(nameof(HasNoTasks));
        }

        private async void ExecuteBackCommand()
        {
            if (CurrentTeam != null)
            {
                if (_navigationService.CanGoBack)
                    _navigationService.GoBack();
            }
        }

        private void ToggleSection(int statusId)
        {
            var section = TaskSections.FirstOrDefault(s => s.StatusId == statusId);
            if (section != null) section.IsExpanded = !section.IsExpanded;
        }

        private void ExecuteCopyInviteCode() => Clipboard.SetText(InviteCode ?? "");

        private async Task ExecuteTransferLeadership()
        {
            AvailableMembers.Clear();
            foreach (var member in Members.Where(m => !m.IsCaptain))
                AvailableMembers.Add(member);

            if (!AvailableMembers.Any())
            {
                await ShowErrorAsync("В команде нет других участников для передачи прав.");
                return;
            }

            SelectedNewCaptain = AvailableMembers.FirstOrDefault();
            ShowTransferDialog = true;
        }

        private void ExecuteCancelTransfer()
        {
            ShowTransferDialog = false;
            SelectedNewCaptain = null;
        }

        private void ExecuteCancelCreateRepo()
        {
            ShowCreateRepoDialog = false;
            NewRepoName = string.Empty;
            NewRepoDescription = string.Empty;
        }

        private void ExecuteCreateTask()
        {
            doDispose = false;
            if (CurrentTeam?.Id != null)
                _navigationService.NavigateTo(new EditTaskPage(CurrentTeam.Id, true));
        }

        private async Task ExecuteEditTask(TaskDto task)
        {
            if (task != null)
            {
                var taskDetails = await _taskService.GetTaskDetailsAsync(task.Id);

                if (!taskDetails.Success || taskDetails.Data == null)
                    return;

                doDispose = false;
                _navigationService.NavigateTo(new EditTaskPage(taskDetails.Data, false));
            }
        }

        private async Task ExecuteOpenTaskAsync(TaskDto task)
        {
            if (task == null) return;

            var taskDetails = await _taskService.GetTaskDetailsAsync(task.Id);
            if (taskDetails.Success && taskDetails.Data != null)
            {
                doDispose = false;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    _navigationService.NavigateTo(new TaskDetailsPage(taskDetails.Data)));
            }
            else
            {
                await ShowErrorAsync($"Задача не найдена или была удалена.\n{taskDetails.Message}");
            }
        }

        private async Task ExecuteDeleteTaskAsync(TaskDto task)
        {
            if (task == null) return;

            var result = await ShowConfirmationAsync($"Вы уверены, что хотите удалить задачу \"{task.Title}\"?", "Подтверждение удаления");

            if (result)
            {
                var deleteResult = await _taskService.DeleteTaskAsync(task.Id);

                if (deleteResult.Success)
                    await ShowSuccessAsync(deleteResult.Message);
                else
                    await ShowErrorAsync(deleteResult.Message);

                if (deleteResult.Success)
                    await LoadTasksAsync();
            }
        }

        private async Task ExecuteLeaveTeamAsync()
        {
            var message = "Вы уверены, что хотите покинуть команду?";
            if (HasGitHubRepo && IsCaptain)
                message += "\n\n⚠️ Внимание: GitHub репозиторий команды будет отсоединен!";

            var result = await ShowConfirmationAsync(message, "Подтверждение выхода");

            if (!result) return;

            var leaveResult = await _teamService.LeaveTeamAsync();

            if (leaveResult.Success)
                await ShowSuccessAsync(leaveResult.Message);
            else
                await ShowErrorAsync(leaveResult.Message);

            if (leaveResult.Success)
            {
                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    if (mainWindow.DataContext is MainWindowViewModel mainViewModel)
                    {
                        await mainViewModel.OpenMainPage();
                    }
                }
            }
        }

        private async Task ExecuteKickMemberAsync(MemberDto member)
        {
            var result = await ShowConfirmationAsync($"Вы уверены, что хотите выгнать участника {member.Username} из команды?", "Подтверждение");

            if (!result) return;

            var kickResult = await _teamService.KickMemberAsync(member.Id);

            if (kickResult.Success)
                await ShowSuccessAsync(kickResult.Message);
            else
                await ShowErrorAsync(kickResult.Message);

            if (kickResult.Success)
                await RefreshTeamDataAsync(CurrentTeam?.Id);
        }

        private async Task ExecuteConfirmTransferAsync()
        {
            if (SelectedNewCaptain == null)
            {
                await ShowErrorAsync("Выберите участника для передачи прав.");
                return;
            }

            var message = $"Вы уверены, что хотите передать права капитана участнику {SelectedNewCaptain.Username}?";
            if (HasGitHubRepo) message += "\n\n⚠️ GitHub репозиторий будет отсоединен!";

            var result = await ShowConfirmationAsync(message, "Подтверждение передачи прав");

            if (!result) return;

            var transferResult = await _teamService.AssignCaptainAsync(CurrentTeam.Id, SelectedNewCaptain.Id);
            if (transferResult.Success)
            {
                await ShowSuccessAsync($"Права капитана переданы {SelectedNewCaptain.Username}");
                ShowTransferDialog = false;
                await RefreshTeamDataAsync(IsOrganizer ? CurrentTeam.Id : null);
            }
            else
            {
                await ShowErrorAsync(transferResult.Message);
            }
        }

        private async Task CreateGitHubRepositoryAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (string.IsNullOrEmpty(user.Data?.GitHubUsername))
            {
                await ShowErrorAsync("Для создания репозитория необходимо сначала привязать GitHub аккаунт в профиле");
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
                await ShowErrorAsync("Введите название репозитория");
                return;
            }

            var result = await _teamService.CreateGitHubRepositoryAsync(
                CurrentTeam.Id, NewRepoName, NewRepoDescription, NewRepoIsPrivate);

            if (result.Success)
            {
                await ShowSuccessAsync($"{result.Message}\n\nURL: {result.Data.RepoUrl}");
                ShowCreateRepoDialog = false;
                await RefreshTeamDataAsync(null);
            }
            else
            {
                await ShowErrorAsync(result.Message);
            }
        }

        private async Task ExecuteGitHubCommandAsync()
        {
            if (HasGitHubRepo)
            {
                if (!string.IsNullOrEmpty(GitHubRepoUrl))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = GitHubRepoUrl, UseShellExecute = true });
            }
            else if (IsCaptain)
            {
                await CreateGitHubRepositoryAsync();
            }
            else
            {
                await ShowErrorAsync("Только капитан команды может подключать GitHub репозиторий");
            }
        }

        private async Task OpenTeamChatAsync()
        {
            if (CurrentTeam?.ChatId == null)
            {
                await ShowErrorAsync("Чат команды не найден");
                return;
            }

            doDispose = false;
            var chat = await _chatService.GetTeamChatAsync(CurrentTeam.Id);
            if (chat.Success)
            {
                _navigationService.NavigateTo(new ChatPage(chat.Data, true));
            }
            else
            {
                doDispose = true;
                await ShowErrorAsync($"Не удалось открыть чат команды:\n{chat.Message}");
            }
        }

        private async Task ExecuteViewMemberProfileAsync(MemberDto member)
        {
            if (member == null) return;

            doDispose = false;
            await Application.Current.Dispatcher.InvokeAsync(() =>
                _navigationService.NavigateTo(new ProfilePage(member.Id)));
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose) return;
            base.DisposeManagedResources();

            Members?.Clear();
            TaskSections?.Clear();
            AvailableMembers?.Clear();
            FinalTeamMembers?.Clear();

            if (_teamService is IDisposable teamDisposable) teamDisposable.Dispose();
            if (_taskService is IDisposable taskDisposable) taskDisposable.Dispose();
            if (_userService is IDisposable userDisposable) userDisposable.Dispose();
        }
    }

    public class TaskSection : BaseViewModel
    {
        private int _statusId;
        public int StatusId
        {
            get => _statusId;
            set => SetProperty(ref _statusId, value);
        }

        private string _statusName;
        public string StatusName
        {
            get => _statusName;
            set => SetProperty(ref _statusName, value);
        }

        private ObservableCollection<TaskDto> _tasks = new();
        public ObservableCollection<TaskDto> Tasks
        {
            get => _tasks;
            set => SetProperty(ref _tasks, value);
        }

        private int _myTasksCount;
        public int MyTasksCount
        {
            get => _myTasksCount;
            set
            {
                SetProperty(ref _myTasksCount, value);
                OnPropertyChanged(nameof(HasMyTasks));
            }
        }

        public bool HasMyTasks => MyTasksCount > 0;

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            Tasks?.Clear();
        }
    }
}