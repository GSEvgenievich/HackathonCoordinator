using HackathonCoordinator.ServiceLayer;
using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
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

        private readonly TeamService _teamService;
        private readonly CompetitionService _competitionService;
        private readonly TaskService _taskService;
        private readonly ChatService _chatService;
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;

        private TeamDto _currentTeam;
        public TeamDto CurrentTeam
        {
            get => _currentTeam;
            set { _currentTeam = value; OnPropertyChanged(); }
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
        private UserDto _currentUser;
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

        private bool _isArchived;
        public bool IsArchived
        {
            get => _isArchived;
            set
            {
                _isArchived = value;
                OnPropertyChanged();
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

        public string PlaceDisplay
        {
            get
            {
                if (!Place.HasValue) return "—";
                return Place.Value switch
                {
                    1 => "🥇 1 место",
                    2 => "🥈 2 место",
                    3 => "🥉 3 место",
                    _ => $"{Place.Value} место"
                };
            }
        }

        public Brush PlaceBrush
        {
            get
            {
                return Place switch
                {
                    1 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Золотой
                    2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)), // Серебряный
                    3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)),  // Бронзовый
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))  // Серый
                };
            }
        }

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
        public ICommand KickMemberCommand { get; }
        public ICommand ViewMemberProfileCommand { get; }

        public TeamViewModel()
        {
            _teamService = new TeamService();
            _competitionService = new CompetitionService();
            _userService = new UserService();
            _chatService = new ChatService();
            _taskService = new TaskService();
            _navigationService = App.NavigationService;

            LeaveTeamCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteLeaveTeamAsync(),
                canExecute: () => CurrentTeam != null);

            GitHubCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteGitHubCommandAsync(),
                canExecute: () => CurrentTeam != null);

            OpenTeamChatCommand = new AsyncRelayCommand(
                execute: async () => await OpenTeamChat(),
                canExecute: () => CurrentTeam?.ChatId != null);

            ConfirmTransferCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteConfirmTransferAsync(),
                canExecute: () => SelectedNewCaptain != null && CurrentTeam != null);

            CreateGitHubRepoCommand = new AsyncRelayCommand(
                execute: async () => await ExecuteCreateGitHubRepoAsync(),
                canExecute: () => CurrentTeam != null && !string.IsNullOrWhiteSpace(NewRepoName));

            KickMemberCommand = new AsyncRelayCommand<MemberDto>(
                execute: async (member) => await ExecuteKickMemberAsync(member),
                canExecute: (member) => member != null && !member.IsCurrentUser && !member.IsCaptain);

            CreateTaskCommand = new RelayCommand(() => ExecuteCreateTask(),
                () => CurrentTeam?.Id != null && (IsCaptainOrOrganizer || CurrentTeam?.Tasks.Count < 10));

            EditTaskCommand = new RelayCommand<TaskDto>(
                task => ExecuteEditTask(task),
                task => task != null && (IsCaptainOrOrganizer || task.AssignedToId == _currentUser?.Id));

            DeleteTaskCommand = new RelayCommand<TaskDto>(
                task => ExecuteDeleteTask(task),
                task => task != null && IsCaptainOrOrganizer);

            OpenTaskCommand = new RelayCommand<TaskDto>(
                task => ExecuteOpenTask(task),
                task => task != null);

            ViewMemberProfileCommand = new AsyncRelayCommand<MemberDto>(
                execute: async (member) => await ExecuteViewMemberProfileAsync(member),
                canExecute: (member) => member != null);

            ToggleSectionCommand = new RelayCommand<int>(
                statusId => ToggleSection(statusId));

            CopyInviteCodeCommand = new RelayCommand(
                () => ExecuteCopyInviteCode(),
                () => !string.IsNullOrEmpty(InviteCode));

            TransferLeadershipCommand = new RelayCommand(
                () => ExecuteTransferLeadership(),
                () => IsCaptain || IsOrganizer && Members.Count > 0);

            CancelTransferCommand = new RelayCommand(
                () => ExecuteCancelTransfer());

            BackCommand = new RelayCommand(
                ExecuteBackCommand,
                () => CurrentTeam != null);

            CancelCreateRepoCommand = new RelayCommand(
                ExecuteCancelCreateRepo);
        }

        private async Task ExecuteLeaveTeamAsync()
        {
            var message = "Вы уверены, что хотите покинуть команду?";

            if (HasGitHubRepo && IsCaptain)
            {
                message += "\n\n⚠️ Внимание: GitHub репозиторий команды будет отсоединен из-за ограничения доступа!";
            }

            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(message, "Подтверждение выхода",
                                      MessageBoxButton.YesNo, MessageBoxImage.Warning);
            });

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var leaveResult = await _teamService.LeaveTeamAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(leaveResult.Message,
                        leaveResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        leaveResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (leaveResult.Success)
                    {
                        _navigationService.NavigateTo(new CompetitionsPage());
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка при выходе из команды: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task ExecuteKickMemberAsync(MemberDto member)
        {
            try
            {
                var result = await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    return MessageBox.Show(
                        $"Вы уверены, что хотите выгнать участника {member.Username} из команды?",
                        "Подтверждение выгона",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                });

                if (result != MessageBoxResult.Yes)
                    return;

                var kickResult = await _teamService.KickMemberAsync(member.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(kickResult.Message,
                        kickResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        kickResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (kickResult.Success)
                    {
                        // Обновляем данные команды
                        LoadTeamDataAsync(CurrentTeam?.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка при выгоне участника: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async Task LoadTeamDataAsync(int? teamId)
        {
            try
            {
                ApiResponse<TeamDto> teamResponse;

                if (teamId == null)
                {
                    teamResponse = await _teamService.GetCurrentTeamAsync();
                }
                else
                {
                    teamResponse = await _teamService.GetTeamByIdAsync(teamId.Value);
                }

                if (!teamResponse.Success || teamResponse.Data == null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("Команда не найдена", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        _navigationService.NavigateTo(new CompetitionsPage());
                    });
                    return;
                }

                CurrentTeam = teamResponse.Data;

                // Проверяем статус соревнования
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
                    // Загружаем финальный состав и результат
                    await LoadFinalTeamDataAsync();
                }
                else
                {
                    await LoadMembersAsync();
                    await LoadTasksAsync();
                }

                // Загружаем результат команды (если есть)
                if (HasResult)
                {
                    await LoadTeamResultAsync();
                }

                CheckUserRole();
                UpdateAllProperties();
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки данных команды: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    _navigationService.NavigateTo(new CompetitionsPage());
                });
            }
        }

        private async Task LoadFinalTeamDataAsync()
        {
            try
            {
                var finalMembers = await _teamService.GetFinalTeamMembersAsync(CurrentTeam.Id);
                if (finalMembers.Success && finalMembers.Data.Any())
                {
                    FinalTeamMembers = new ObservableCollection<FinalTeamMemberDto>(finalMembers.Data);

                    // Формируем Members из финального состава
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

        private async void ExecuteBackCommand()
        {
            if (CurrentTeam != null)
            {
                var competition = (await _competitionService.GetCompetitionAsync(CurrentTeam.CompetitionId)).Data;
                _navigationService.NavigateTo(new CompetitionDetailsPage(competition));
            }
        }

        private async Task CreateGitHubRepositoryAsync()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (string.IsNullOrEmpty(user.Data.GitHubUsername))
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
                        $"{result.Message}\n\nURL репозитория: {result.Data.RepoUrl}");
                    ShowCreateRepoDialog = false;

                    await LoadTeamDataAsync(null);
                }
                else
                {
                    ShowErrorMessage("Ошибка создания репозитория", result.Message);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Ошибка", $"Неожиданная ошибка: {ex.Message}");
            }
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

        private async Task OpenTeamChat()
        {
            if (CurrentTeam?.ChatId != null)
            {
                var chat = await _chatService.GetTeamChatAsync(CurrentTeam.Id);

                if (chat.Success)
                {
                    var chatPage = new ChatPage();
                    var viewModel = chatPage.DataContext as ChatViewModel;
                    if (viewModel != null)
                    {
                        await viewModel.LoadTeamChatAsync(chat.Data);
                        _navigationService.NavigateTo(chatPage);
                    }
                }
                else
                {
                    MessageBox.Show($"Не удалось открыть чат команды:\n{chat.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Чат команды не найден");
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

        private async Task ExecuteViewMemberProfileAsync(MemberDto member)
        {
            if (member == null) return;

            try
            {
                doDispose = false;
                var profilePage = new ProfilePage(member.Id);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _navigationService.NavigateTo(profilePage);
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка открытия профиля: {ex.Message}");
                doDispose = true;
            }
        }

        private void ExecuteTransferLeadership()
        {
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

                    if (IsOrganizer)
                        await LoadTeamDataAsync(CurrentTeam.Id);
                    else
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

            var taskSections = new System.Collections.Generic.List<TaskSection>
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

        private void CheckUserRole()
        {
            IsCaptain = _currentUser.RoleId == (int)Roles.Captain;
            IsOrganizer = _currentUser.RoleId == (int)Roles.Organizer ||
                          _currentUser.RoleId == (int)Roles.Admin;

            OnPropertyChanged(nameof(IsCaptain));
            OnPropertyChanged(nameof(IsOrganizer));
            OnPropertyChanged(nameof(IsCaptainOrOrganizer));
            OnPropertyChanged(nameof(IsTeamMember));
        }

        private async Task ShowErrorAsync(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void UpdateDerivedProperties()
        {
            OnPropertyChanged(nameof(TeamName));
            OnPropertyChanged(nameof(InviteCode));
            OnPropertyChanged(nameof(GitHubRepoUrl));
            OnPropertyChanged(nameof(HasGitHubRepo));
            OnPropertyChanged(nameof(CanConnectGitHub));
            OnPropertyChanged(nameof(GitHubButtonText));
            OnPropertyChanged(nameof(IsGitHubButtonVisible));
        }

        private void UpdateAllProperties()
        {
            UpdateDerivedProperties();
            OnPropertyChanged(nameof(MembersCount));
            OnPropertyChanged(nameof(TotalTasksCount));
            OnPropertyChanged(nameof(CompletedTasksCount));
            OnPropertyChanged(nameof(HasNoTasks));
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose)
                return;

            base.DisposeManagedResources();

            Members?.Clear();
            TaskSections?.Clear();
            AvailableMembers?.Clear();

            CurrentTeam = null;
            _currentUser = null;
            SelectedNewCaptain = null;
            NewRepoName = null;
            NewRepoDescription = null;

            if (_teamService is IDisposable teamDisposable)
                teamDisposable.Dispose();

            if (_taskService is IDisposable taskDisposable)
                taskDisposable.Dispose();

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();
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