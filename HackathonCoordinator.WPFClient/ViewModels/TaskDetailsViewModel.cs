using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class TaskDetailsViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectService _projectService;
        private readonly UserService _userService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private TaskDetailsDto _task;
        public TaskDetailsDto Task
        {
            get => _task;
            set { _task = value; OnPropertyChanged(); }
        }

        private int _currentUserId;

        public bool IsMyTask => Task?.AssignedToId == _currentUserId;
        public bool CanAssignTask => Task?.CanAssign ?? false;
        public bool CanEditTask => Task?.CanEdit ?? false;
        public bool CanCompleteTask => Task?.CanComplete ?? false;
        public bool CanCancelTask => Task?.CanCancel ?? false;
        public bool HasChat => Task?.HasChat ?? false;

        public RelayCommand BackCommand { get; }
        public RelayCommand EditTaskCommand { get; }
        public RelayCommand AssignTaskCommand { get; }
        public RelayCommand CompleteTaskCommand { get; }
        public RelayCommand CancelTaskCommand { get; }
        public RelayCommand OpenTaskChatCommand { get; }

        public TaskDetailsViewModel()
        {
            _navigationService = App.NavigationService;
            _projectService = new ProjectService();
            _userService = new UserService();

            BackCommand = new RelayCommand(() => _navigationService.GoBack());
            EditTaskCommand = new RelayCommand(EditTask);
            AssignTaskCommand = new RelayCommand(async () => await AssignTaskAsync());
            CompleteTaskCommand = new RelayCommand(async () => await CompleteTaskAsync());
            CancelTaskCommand = new RelayCommand(async () => await CancelTaskAsync());
            OpenTaskChatCommand = new RelayCommand(OpenTaskChat);

            LoadCurrentUser();
        }

        private async void LoadCurrentUser()
        {
            var user = await _userService.GetCurrentUserAsync();
            _currentUserId = user?.Id ?? 0;
        }

        public async void LoadTaskData(int taskId)
        {
            var task = await _projectService.GetTaskDetailsAsync(taskId);
            if (task != null)
            {
                Task = task;
                UpdatePermissions();
            }
        }

        private void UpdatePermissions()
        {
            OnPropertyChanged(nameof(IsMyTask));
            OnPropertyChanged(nameof(CanAssignTask));
            OnPropertyChanged(nameof(CanEditTask));
            OnPropertyChanged(nameof(CanCompleteTask));
            OnPropertyChanged(nameof(CanCancelTask));
            OnPropertyChanged(nameof(HasChat));
        }

        private void EditTask()
        {
            if (Task != null)
            {
                _navigationService.NavigateTo(new EditTaskPage(Task.Id, false));
            }
        }

        private async Task AssignTaskAsync()
        {
            if (Task == null) return;

            // Здесь можно реализовать диалог выбора исполнителя
            // Пока просто назначаем на текущего пользователя
            var result = await _projectService.AssignTaskAsync(Task.Id, _currentUserId);
            MessageBox.Show(result.Message);

            if (result.Success)
            {
                // Обновляем данные задачи
                LoadTaskData(Task.Id);
            }
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
                var completionResult = await _projectService.RequestCompletionAsync(Task.Id);
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
                var cancellationResult = await _projectService.RequestCancellationAsync(Task.Id);
                MessageBox.Show(cancellationResult.Message);

                if (cancellationResult.Success)
                {
                    LoadTaskData(Task.Id);
                }
            }
        }

        private void OpenTaskChat()
        {
            if (Task?.TaskChatId != null)
            {
                MessageBox.Show($"Открытие чата задачи (ID: {Task.TaskChatId})");
                // Реализовать переход в чат
            }
        }
    }
}