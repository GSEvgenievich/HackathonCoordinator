using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class TaskDetailsViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
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
        public bool CanOpenChat => IsMyTask || CurrentUser?.RoleId == 1 || CurrentUser?.RoleId == 3;

        // AsyncRelayCommand для операций с API
        public ICommand BackCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand OpenAssignmentDialogCommand { get; }
        public ICommand AssignTaskCommand { get; }
        public ICommand CancelAssignmentCommand { get; }
        public ICommand OpenVotingCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand CancelTaskCommand { get; }
        public ICommand OpenTaskChatCommand { get; }
        public ICommand ConfirmCompletionCommand { get; }
        public ICommand RejectCompletionCommand { get; }
        public ICommand CancelTaskAsCaptainCommand { get; }

        public TaskDetailsViewModel()
        {
            _navigationService = App.NavigationService;
            _userService = new UserService();
            _chatService = new ChatService();
            _taskService = new TaskService();
            _teamService = new TeamService();

            BackCommand = new RelayCommand(BackToTeam);
            EditTaskCommand = new RelayCommand(EditTask);
            OpenAssignmentDialogCommand = new RelayCommand(OpenAssignmentDialog);

            // AsyncRelayCommand для назначения задачи
            AssignTaskCommand = new AsyncRelayCommand(
                execute: async () => await AssignTaskAsync(),
                canExecute: () => Task != null && SelectedAssignee != null && CanAssignTask);

            CancelAssignmentCommand = new RelayCommand(CancelAssignment);
            OpenVotingCommand = new RelayCommand(OpenVotingDialog);

            // AsyncRelayCommand для завершения задачи
            CompleteTaskCommand = new AsyncRelayCommand(
                execute: async () => await CompleteTaskAsync(),
                canExecute: () => Task != null && CanCompleteTask);

            // AsyncRelayCommand для отмены задачи
            CancelTaskCommand = new AsyncRelayCommand(
                execute: async () => await CancelTaskAsync(),
                canExecute: () => Task != null && CanCancelTask);

            // AsyncRelayCommand для открытия чата задачи
            OpenTaskChatCommand = new AsyncRelayCommand(
                execute: async () => await OpenTaskChat(),
                canExecute: () => Task?.TaskChatId != null && CanOpenChat);

            // AsyncRelayCommand для подтверждения завершения
            ConfirmCompletionCommand = new AsyncRelayCommand(
                execute: async () => await ConfirmCompletionAsync(),
                canExecute: () => Task != null && CanConfirmCompletion);

            // AsyncRelayCommand для отклонения завершения
            RejectCompletionCommand = new AsyncRelayCommand(
                execute: async () => await RejectCompletionAsync(),
                canExecute: () => Task != null && CanRejectCompletion);

            // AsyncRelayCommand для отмены задачи капитаном
            CancelTaskAsCaptainCommand = new AsyncRelayCommand(
                execute: async () => await CancelTaskAsCaptainAsync(),
                canExecute: () => Task != null && CanCancelTaskAsCaptain);

            LoadCurrentUser();
        }

        private void BackToTeam()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (CurrentUser?.RoleId != 3)
                    _navigationService.NavigateTo(new TeamPage());
                else
                    _navigationService.NavigateTo(new TeamPage(Task?.TeamId));
            });
        }

        private async void LoadCurrentUser()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                CurrentUser = user.Data;
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки пользователя: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        public async void LoadTaskData(int taskId)
        {
            try
            {
                var task = await _taskService.GetTaskDetailsAsync(taskId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (task.Success)
                    {
                        Task = task.Data;
                        UpdatePermissions();
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка загрузки задачи: {task.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

                if (task.Success)
                {
                    await LoadAvailableAssignees();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки задачи: {ex.Message}\n\nПроверьте подключение к серверу.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
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
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка загрузки участников команды: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
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

        private void EditTask()
        {
            if (Task != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _navigationService.NavigateTo(new EditTaskPage(Task.Id, false));
                });
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

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(result.Message,
                        result.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        result.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (result.Success)
                    {
                        ShowAssignmentDialog = false;
                        LoadTaskData(Task.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка назначения задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void OpenVotingDialog()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Функция голосования будет реализована в следующей версии", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private async Task CompleteTaskAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите запросить завершение этой задачи?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var completionResult = await _taskService.RequestCompletionAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(completionResult.Message,
                        completionResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        completionResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (completionResult.Success)
                    {
                        LoadTaskData(Task.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка запроса завершения задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task CancelTaskAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите запросить отмену этой задачи?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var cancellationResult = await _taskService.RequestCancellationAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(cancellationResult.Message,
                        cancellationResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        cancellationResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (cancellationResult.Success)
                    {
                        LoadTaskData(Task.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка запроса отмены задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task ConfirmCompletionAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите подтвердить завершение этой задачи?",
                    "Подтверждение завершения",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var completionResult = await _taskService.ConfirmCompletionAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(completionResult.Message,
                        completionResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        completionResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (completionResult.Success)
                    {
                        LoadTaskData(Task.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка подтверждения завершения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task RejectCompletionAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите отклонить завершение этой задачи? Задача вернется в работу.",
                    "Отклонение завершения",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var rejectionResult = await _taskService.RejectCompletionAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(rejectionResult.Message,
                        rejectionResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        rejectionResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (rejectionResult.Success)
                    {
                        LoadTaskData(Task.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка отклонения завершения: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task CancelTaskAsCaptainAsync()
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return MessageBox.Show(
                    "Вы уверены, что хотите отменить эту задачу?",
                    "Отмена задачи",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
            });

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var cancellationResult = await _taskService.CancelTaskAsync(Task.Id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(cancellationResult.Message,
                        cancellationResult.Success ? "Успешно" : "Ошибка",
                        MessageBoxButton.OK,
                        cancellationResult.Success ? MessageBoxImage.Information : MessageBoxImage.Error);

                    if (cancellationResult.Success)
                    {
                        LoadTaskData(Task.Id);
                    }
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка отмены задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task OpenTaskChat()
        {
            try
            {
                var chat = await _chatService.GetTaskChatAsync(Task.Id);

                if (chat.Success)
                {
                    var chatPage = new ChatPage();
                    var viewModel = chatPage.DataContext as ChatViewModel;
                    if (viewModel != null)
                    {
                        await viewModel.LoadTaskChatAsync(chat.Data);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _navigationService.NavigateTo(chatPage);
                        });
                    }
                }
                else
                {
                    MessageBox.Show($"Не удалось открыть чат задачи:\n{chat.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Ошибка открытия чата задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        protected override void DisposeManagedResources()
        {
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