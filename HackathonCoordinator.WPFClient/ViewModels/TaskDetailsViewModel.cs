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
    public class TaskDetailsViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly UserService _userService;
        private readonly TaskService _taskService;
        private readonly TeamService _teamService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private TaskDetailsDto _task;
        public TaskDetailsDto Task
        {
            get => _task;
            set
            {
                _task = value;
                OnPropertyChanged();
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
                _currentUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMyTask));
            }
        }
        private bool _showAssignmentDialog;
        private MemberDto _selectedAssignee;

        public bool ShowAssignmentDialog
        {
            get => _showAssignmentDialog;
            set { _showAssignmentDialog = value; OnPropertyChanged(); }
        }

        public MemberDto SelectedAssignee
        {
            get => _selectedAssignee;
            set { _selectedAssignee = value; OnPropertyChanged(); }
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
        public bool HasChat => Task?.HasChat ?? false;

        public RelayCommand BackCommand { get; }
        public RelayCommand EditTaskCommand { get; }
        public RelayCommand OpenAssignmentDialogCommand { get; }
        public RelayCommand AssignTaskCommand { get; }
        public RelayCommand CancelAssignmentCommand { get; }
        public RelayCommand OpenVotingCommand { get; }
        public RelayCommand CompleteTaskCommand { get; }
        public RelayCommand CancelTaskCommand { get; }
        public RelayCommand OpenTaskChatCommand { get; }
        public RelayCommand ConfirmCompletionCommand { get; }
        public RelayCommand RejectCompletionCommand { get; }
        public RelayCommand CancelTaskAsCaptainCommand { get; }

        public TaskDetailsViewModel()
        {
            _navigationService = App.NavigationService;
            _userService = new UserService();
            _taskService = new TaskService();
            _teamService = new TeamService();

            BackCommand = new RelayCommand(BackToTeam);
            EditTaskCommand = new RelayCommand(EditTask);
            OpenAssignmentDialogCommand = new RelayCommand(OpenAssignmentDialog);
            AssignTaskCommand = new RelayCommand(async () => await AssignTaskAsync());
            CancelAssignmentCommand = new RelayCommand(CancelAssignment);
            OpenVotingCommand = new RelayCommand(OpenVotingDialog);
            CompleteTaskCommand = new RelayCommand(async () => await CompleteTaskAsync());
            CancelTaskCommand = new RelayCommand(async () => await CancelTaskAsync());
            OpenTaskChatCommand = new RelayCommand(async () => await OpenTaskChat());
            ConfirmCompletionCommand = new RelayCommand(async () => await ConfirmCompletionAsync());
            RejectCompletionCommand = new RelayCommand(async () => await RejectCompletionAsync());
            CancelTaskAsCaptainCommand = new RelayCommand(async () => await CancelTaskAsCaptainAsync());

            LoadCurrentUser();
        }

        private void BackToTeam()
        {
            if (CurrentUser.RoleId != 3)
                _navigationService.NavigateTo(new TeamPage());
            else
                _navigationService.NavigateTo(new TeamPage(Task.TeamId));
        }

        private async void LoadCurrentUser()
        {
            var user = await _userService.GetCurrentUserAsync();
            CurrentUser = user.Data;
        }

        public async void LoadTaskData(int taskId)
        {
            var task = await _taskService.GetTaskDetailsAsync(taskId);
            if (task.Success)
            {
                Task = task.Data;
                await LoadAvailableAssignees();
                UpdatePermissions();
            }
        }

        private async Task LoadAvailableAssignees()
        {
            if (Task?.TeamId == null) return;

            var team = await _teamService.GetCurrentTeamAsync();
            if (team.Success)
            {
                if (team.Data.Members != null)
                {
                    AvailableAssignees.Clear();
                    foreach (var member in team.Data.Members)
                    {
                        AvailableAssignees.Add(member);
                    }

                    SelectedAssignee = AvailableAssignees.FirstOrDefault();
                }
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
            OnPropertyChanged(nameof(HasChat));
        }

        private void EditTask()
        {
            if (Task != null)
            {
                _navigationService.NavigateTo(new EditTaskPage(Task.Id, false));
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
            if (Task == null || SelectedAssignee == null)
            {
                MessageBox.Show("Выберите исполнителя для задачи");
                return;
            }

            var result = await _taskService.AssignTaskAsync(Task.Id, SelectedAssignee.Id);
            MessageBox.Show(result.Message);

            if (result.Success)
            {
                ShowAssignmentDialog = false;
                // Обновляем данные задачи
                LoadTaskData(Task.Id);
            }
        }

        private void OpenVotingDialog()
        {
            MessageBox.Show("Функция голосования будет реализована в следующей версии");
        }

        private async Task CompleteTaskAsync()
        {
            if (Task == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите запросить завершение этой задачи?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var completionResult = await _taskService.RequestCompletionAsync(Task.Id);
                MessageBox.Show(completionResult.Message);

                if (completionResult.Success)
                {
                    LoadTaskData(Task.Id);
                }
            }
        }

        private async Task CancelTaskAsync()
        {
            if (Task == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите запросить отмену этой задачи?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var cancellationResult = await _taskService.RequestCancellationAsync(Task.Id);
                MessageBox.Show(cancellationResult.Message);

                if (cancellationResult.Success)
                {
                    LoadTaskData(Task.Id);
                }
            }
        }

        private async Task ConfirmCompletionAsync()
        {
            if (Task == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите подтвердить завершение этой задачи?",
                "Подтверждение завершения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var completionResult = await _taskService.ConfirmCompletionAsync(Task.Id);
                MessageBox.Show(completionResult.Message);

                if (completionResult.Success)
                {
                    LoadTaskData(Task.Id);
                }
            }
        }

        private async Task RejectCompletionAsync()
        {
            if (Task == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите отклонить завершение этой задачи? Задача вернется в работу.",
                "Отклонение завершения",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var rejectionResult = await _taskService.RejectCompletionAsync(Task.Id);
                MessageBox.Show(rejectionResult.Message);

                if (rejectionResult.Success)
                {
                    LoadTaskData(Task.Id);
                }
            }
        }

        private async Task CancelTaskAsCaptainAsync()
        {
            if (Task == null) return;

            var result = MessageBox.Show(
                "Вы уверены, что хотите отменить эту задачу?",
                "Отмена задачи",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var cancellationResult = await _taskService.CancelTaskAsync(Task.Id);
                MessageBox.Show(cancellationResult.Message);

                if (cancellationResult.Success)
                {
                    LoadTaskData(Task.Id);
                }
            }
        }

        private async Task OpenTaskChat()
        {
            if (Task?.TaskChatId != null)
            {
                var chatPage = new ChatPage();
                var viewModel = chatPage.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    await viewModel.LoadTaskChatAsync(Task.Id);
                    _navigationService.NavigateTo(chatPage);
                }
            }
            else
            {
                MessageBox.Show("Чат задачи не найден");
            }
        }
    }
}