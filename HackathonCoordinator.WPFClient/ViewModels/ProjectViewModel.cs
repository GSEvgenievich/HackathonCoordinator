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
    public class ProjectViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectService _projectService;
        private readonly TeamService _teamService;
        private readonly UserService _userService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private ProjectDto _project;
        public ProjectDto Project
        {
            get => _project;
            set { _project = value; OnPropertyChanged(); }
        }

        private bool _isCaptain;
        public bool IsCaptain
        {
            get => _isCaptain;
            set { _isCaptain = value; OnPropertyChanged(); }
        }

        public int TotalTasksCount => TaskSections.Sum(s => s.Tasks.Count);
        public int CompletedTasksCount => TaskSections.FirstOrDefault(s => s.StatusId == 4)?.Tasks.Count ?? 0;
        public bool HasTasks => TotalTasksCount > 0;

        public ObservableCollection<TaskSection> TaskSections { get; set; } = new();

        public ICommand BackCommand { get; }
        public ICommand OpenChatCommand { get; }
        public ICommand CreateTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ToggleSectionCommand { get; }
        public ICommand EditProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand OpenTaskCommand { get; }

        public ProjectViewModel()
        {
            _navigationService = App.NavigationService;
            _projectService = new ProjectService();
            _userService = new UserService();
            _teamService = new TeamService();

            BackCommand = new RelayCommand(() => _navigationService.NavigateTo(new TeamPage()));
            OpenChatCommand = new RelayCommand(OpenChat);
            EditProjectCommand = new RelayCommand(EditProject);
            DeleteProjectCommand = new RelayCommand(async () => await DeleteProjectAsync());
            CreateTaskCommand = new RelayCommand(CreateTask);
            EditTaskCommand = new RelayCommand<TaskDto>(EditTask);
            DeleteTaskCommand = new RelayCommand<TaskDto>(DeleteTask);
            ToggleSectionCommand = new RelayCommand<int>(ToggleSection); 
            OpenTaskCommand = new RelayCommand<TaskDto>(OpenTask);
        }

        private async Task DeleteProjectAsync()
        {
            var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить проект \"{Project.Name}\"?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _projectService.DeleteProjectAsync(Project.Id);
                _navigationService.NavigateTo(new TeamPage());
            }
        }

        private void OpenTask(TaskDto task)
        {
            if (task != null)
            {
                _navigationService.NavigateTo(new TaskDetailsPage(task.Id));
            }
        }

        private void CreateTask()
        {
            if (Project != null)
            {
                _navigationService.NavigateTo(new EditTaskPage(Project.Id, true));
            }
        }

        public async void LoadProjectData(ProjectDto project)
        {
            if (project != null)
            {
                Project = project;

                await LoadProjectTasksAsync(project.Id);

                await CheckUserRole();
            }
        }

        private async Task LoadProjectTasksAsync(int projectId)
        {
            var tasks = await _projectService.GetProjectTasksAsync(projectId);

            var taskSections = new List<TaskSection>
            {
                new TaskSection { StatusId = 1, StatusName = "В планах", Tasks = new ObservableCollection<TaskDto>() },
                new TaskSection { StatusId = 2, StatusName = "В процессе", Tasks = new ObservableCollection<TaskDto>() },
                new TaskSection { StatusId = 3, StatusName = "На проверке", Tasks = new ObservableCollection<TaskDto>() },
                new TaskSection { StatusId = 4, StatusName = "Завершена", Tasks = new ObservableCollection<TaskDto>() },
                new TaskSection { StatusId = 5, StatusName = "Отменена", Tasks = new ObservableCollection<TaskDto>() }
            };

            foreach (var task in tasks)
            {
                var section = taskSections.FirstOrDefault(s => s.StatusId == task.StatusId);
                section?.Tasks.Add(task);
            }

            TaskSections.Clear();
            foreach (var section in taskSections)
            {
                TaskSections.Add(section);
            }

            OnPropertyChanged(nameof(TotalTasksCount));
            OnPropertyChanged(nameof(CompletedTasksCount));
            OnPropertyChanged(nameof(HasTasks));
        }

        private async Task CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsCaptain = user?.RoleId == 1;
        }

        private void OpenChat()
        {
            MessageBox.Show("Переход в чат проекта будет реализован позже");
        }

        private void EditProject()
        {
            if (Project != null)
            {
                _navigationService.NavigateTo(new EditProjectPage(Project));
            }
            else
            {
                MessageBox.Show("Не удалось определить проект");
            }
        }

        private void EditTask(TaskDto task)
        {
            if (task != null)
            {
                _navigationService.NavigateTo(new EditTaskPage(task.Id, false));
            }
        }

        private void DeleteTask(TaskDto task)
        {
            if (task != null)
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить задачу \"{task.Title}\"?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Логика удаления задачи
                    MessageBox.Show("Задача удалена");
                }
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
    }

    public class TaskSection : INotifyPropertyChanged
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public ObservableCollection<TaskDto> Tasks { get; set; } = new();

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