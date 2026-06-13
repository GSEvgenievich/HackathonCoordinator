using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Helpers;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class TaskDetailsViewModel : BaseViewModel
    {
        public bool doDispose = true;
        private bool _isInitialized = false;
        private int _taskId;

        private readonly UserService _userService;
        private readonly ChatService _chatService;
        private readonly TaskService _taskService;
        private readonly TeamService _teamService;

        private TaskDetailsDto _task;
        public TaskDetailsDto Task
        {
            get => _task;
            set
            {
                SetProperty(ref _task, value);
                OnPropertyChanged(nameof(DisplayAssignedTo));
                OnPropertyChanged(nameof(DisplayGitHubBranch));
                UpdatePermissions();
            }
        }

        private UserDto _currentUser;
        public UserDto CurrentUser
        {
            get => _currentUser;
            set
            {
                SetProperty(ref _currentUser, value);
                OnPropertyChanged(nameof(IsMyTask));
                OnPropertyChanged(nameof(CanOpenChat));
            }
        }

        private bool _showAssignmentDialog;
        public bool ShowAssignmentDialog
        {
            get => _showAssignmentDialog;
            set => SetProperty(ref _showAssignmentDialog, value);
        }

        private MemberDto _selectedAssignee;
        public MemberDto SelectedAssignee
        {
            get => _selectedAssignee;
            set => SetProperty(ref _selectedAssignee, value);
        }

        public string DisplayAssignedTo =>
            string.IsNullOrEmpty(Task?.AssignedToUsername) ? "Не назначен" : Task.AssignedToUsername;

        public string DisplayGitHubBranch =>
            string.IsNullOrEmpty(Task?.GitHubBranchName) ? "Не указана" : Task.GitHubBranchName;

        public ObservableCollection<MemberDto> AvailableAssignees { get; } = new();

        public bool IsMyTask => Task?.AssignedToId == CurrentUser?.Id;
        public bool CanAssignTask => Task?.CanAssign ?? false;
        public bool CanEditTask => Task?.CanEdit ?? false;
        public bool CanCompleteTask => Task?.CanComplete ?? false;
        public bool CanCancelTask => Task?.CanCancel ?? false;
        public bool CanConfirmCompletion => Task?.CanConfirmCompletion ?? false;
        public bool CanRejectCompletion => Task?.CanRejectCompletion ?? false;
        public bool CanCancelTaskAsCaptain => Task?.CanCancelTaskAsCaptain ?? false;
        public bool CanOpenChat => IsMyTask || CurrentUser?.RoleId == (int)Roles.Captain ||
                                    CurrentUser?.RoleId == (int)Roles.Organizer ||
                                    CurrentUser?.RoleId == (int)Roles.Admin;

        // Команды
        public ICommand BackCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand OpenAssignmentDialogCommand { get; }
        public ICommand AssignTaskCommand { get; }
        public ICommand CancelAssignmentCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand CancelTaskCommand { get; }
        public ICommand OpenTaskChatCommand { get; }
        public ICommand ConfirmCompletionCommand { get; }
        public ICommand RejectCompletionCommand { get; }
        public ICommand CancelTaskAsCaptainCommand { get; }

        public TaskDetailsViewModel()
        {
            _userService = new UserService();
            _chatService = new ChatService();
            _taskService = new TaskService();
            _teamService = new TeamService();

            BackCommand = new RelayCommand(BackToTeam);
            EditTaskCommand = new AsyncRelayCommand(EditTask);
            OpenAssignmentDialogCommand = new RelayCommand(OpenAssignmentDialog);
            CancelAssignmentCommand = new RelayCommand(CancelAssignment);

            AssignTaskCommand = new AsyncRelayCommand(
                execute: async () => await AssignTaskAsync(),
                canExecute: () => Task != null && SelectedAssignee != null && CanAssignTask);

            CompleteTaskCommand = new AsyncRelayCommand(
                execute: async () => await CompleteTaskAsync(),
                canExecute: () => Task != null && CanCompleteTask);

            CancelTaskCommand = new AsyncRelayCommand(
                execute: async () => await CancelTaskAsync(),
                canExecute: () => Task != null && CanCancelTask);

            OpenTaskChatCommand = new AsyncRelayCommand(
                execute: async () => await OpenTaskChatAsync(),
                canExecute: () => Task?.TaskChatId != null && CanOpenChat);

            ConfirmCompletionCommand = new AsyncRelayCommand(
                execute: async () => await ConfirmCompletionAsync(),
                canExecute: () => Task != null && CanConfirmCompletion);

            RejectCompletionCommand = new AsyncRelayCommand(
                execute: async () => await RejectCompletionAsync(),
                canExecute: () => Task != null && CanRejectCompletion);

            CancelTaskAsCaptainCommand = new AsyncRelayCommand(
                execute: async () => await CancelTaskAsCaptainAsync(),
                canExecute: () => Task != null && CanCancelTaskAsCaptain);

            LoadCurrentUser();
        }

        public async Task InitializeAsync(TaskDetailsDto task)
        {
            if (_isInitialized && Task.Id == task.Id) return;

            IsLoading = true;

            try
            {
                Task = task;
                await LoadAvailableAssignees();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки задачи: {ex.Message}");
                BackToTeam();
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RefreshAsync()
        {
            await LoadTaskData(Task.Id);
        }

        private async Task LoadTaskData(int taskId)
        {
            IsLoading = true;

            try
            {
                var task = await _taskService.GetTaskDetailsAsync(taskId);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (task.Success)
                    {
                        Task = task.Data;
                    }
                    else
                    {
                        await ShowErrorAsync($"Ошибка загрузки задачи: {task.Message}");
                        BackToTeam();
                    }
                });

                if (task.Success)
                {
                    await LoadAvailableAssignees();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ShowErrorAsync($"Ошибка загрузки задачи: {ex.Message}");
                    BackToTeam();
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadCurrentUser()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user.Success)
                {
                    CurrentUser = user.Data;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки пользователя: {ex.Message}");
            }
        }

        private async Task LoadAvailableAssignees()
        {
            if (Task?.TeamId == null) return;

            try
            {
                var team = await _teamService.GetCurrentTeamAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (team.Success && team.Data.Members != null)
                    {
                        AvailableAssignees.Clear();
                        foreach (var member in team.Data.Members)
                        {
                            AvailableAssignees.Add(member);
                        }

                        SelectedAssignee = AvailableAssignees.FirstOrDefault();
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка загрузки участников команды: {ex.Message}");
            }
        }

        private void UpdatePermissions()
        {
            OnPropertyChanged(nameof(IsMyTask));
            OnPropertyChanged(nameof(CanAssignTask));
            OnPropertyChanged(nameof(CanEditTask));
            OnPropertyChanged(nameof(CanCompleteTask));
            OnPropertyChanged(nameof(CanCancelTask));
            OnPropertyChanged(nameof(CanConfirmCompletion));
            OnPropertyChanged(nameof(CanRejectCompletion));
            OnPropertyChanged(nameof(CanCancelTaskAsCaptain));
            OnPropertyChanged(nameof(CanOpenChat));
        }

        private void BackToTeam()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _navigationService.GoBack();
            });
        }

        private async Task EditTask()
        {
            if (Task != null)
            {
                doDispose = false;

                var taskDetails = await _taskService.GetTaskDetailsAsync(Task.Id);

                if (taskDetails.Success && taskDetails.Data != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _navigationService.NavigateTo(new EditTaskPage(taskDetails.Data, false));
                    });
                }
            }
        }

        private void OpenAssignmentDialog()
        {
            ShowAssignmentDialog = true;
        }

        private void CancelAssignment()
        {
            ShowAssignmentDialog = false;
            SelectedAssignee = null;
        }

        private async Task AssignTaskAsync()
        {
            try
            {
                var result = await _taskService.AssignTaskAsync(Task.Id, SelectedAssignee.Id);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (result.Success)
                        await ShowSuccessAsync(result.Message);
                    else
                        await ShowErrorAsync(result.Message);
                });

                if (result.Success)
                {
                    ShowAssignmentDialog = false;
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка назначения задачи: {ex.Message}");
            }
        }

        private async Task CompleteTaskAsync()
        {
            var result = await ShowYesNoCancelAsync("Вы уверены, что хотите запросить завершение этой задачи?");

            if (result != true) return;

            try
            {
                var completionResult = await _taskService.RequestCompletionAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (completionResult.Success)
                        await ShowSuccessAsync(completionResult.Message);
                    else
                        await ShowErrorAsync(completionResult.Message);

                    if (completionResult.Success)
                    {
                        await RefreshAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка запроса завершения задачи: {ex.Message}");
            }
        }

        private async Task CancelTaskAsync()
        {
            var result = await ShowYesNoCancelAsync("Вы уверены, что хотите запросить отмену этой задачи?");

            if (result != true) return;

            try
            {
                var cancellationResult = await _taskService.RequestCancellationAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (cancellationResult.Success)
                        await ShowSuccessAsync(cancellationResult.Message);
                    else
                        await ShowErrorAsync(cancellationResult.Message);

                    if (cancellationResult.Success)
                    {
                        await RefreshAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка запроса отмены задачи: {ex.Message}");
            }
        }

        private async Task ConfirmCompletionAsync()
        {
            var result = await ShowYesNoCancelAsync("Вы уверены, что хотите подтвердить завершение этой задачи?");

            if (result != true) return;

            try
            {
                var completionResult = await _taskService.ConfirmCompletionAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (completionResult.Success)
                        await ShowSuccessAsync(completionResult.Message);
                    else
                        await ShowErrorAsync(completionResult.Message);

                    if (completionResult.Success)
                    {
                        await RefreshAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка подтверждения завершения: {ex.Message}");
            }
        }

        private async Task RejectCompletionAsync()
        {
            var result = await ShowYesNoCancelAsync("Вы уверены, что хотите отклонить завершение этой задачи? Задача вернется в работу.","Отклонение завершения");

            if (result != true) return;

            try
            {
                var rejectionResult = await _taskService.RejectCompletionAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (rejectionResult.Success)
                        await ShowSuccessAsync(rejectionResult.Message);
                    else
                        await ShowErrorAsync(rejectionResult.Message); 

                    if (rejectionResult.Success)
                    {
                        await RefreshAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка отклонения завершения: {ex.Message}");
            }
        }

        private async Task CancelTaskAsCaptainAsync()
        {
            var result = await ShowYesNoCancelAsync("Вы уверены, что хотите отменить эту задачу?", "Отмена задачи");
           
            if (result != true) return;

            try
            {
                var cancellationResult = await _taskService.CancelTaskAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (cancellationResult.Success)
                        await ShowSuccessAsync(cancellationResult.Message);
                    else
                        await ShowErrorAsync(cancellationResult.Message);

                    if (cancellationResult.Success)
                    {
                        await RefreshAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка отмены задачи: {ex.Message}");
            }
        }

        private async Task OpenTaskChatAsync()
        {
            try
            {
                doDispose = false;

                var chat = await _chatService.GetTaskChatAsync(Task.Id);

                if (chat.Success)
                {
                    var chatPage = new ChatPage(chat.Data, false);
                    _navigationService.NavigateTo(chatPage);
                }
                else
                {
                    await ShowErrorAsync($"Не удалось открыть чат задачи:\n{chat.Message}");
                    doDispose = true;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка открытия чата задачи: {ex.Message}");
                doDispose = true;
            }
        }

        protected override void DisposeManagedResources()
        {
            if (!doDispose)
                return;

            base.DisposeManagedResources();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Task = null;
                CurrentUser = null;
                SelectedAssignee = null;
                AvailableAssignees?.Clear();
            });

            if (_userService is IDisposable userDisposable)
                userDisposable.Dispose();

            if (_taskService is IDisposable taskDisposable)
                taskDisposable.Dispose();

            if (_teamService is IDisposable teamDisposable)
                teamDisposable.Dispose();
        }
    }
}