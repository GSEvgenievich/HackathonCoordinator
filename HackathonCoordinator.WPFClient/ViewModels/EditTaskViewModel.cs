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
    public class EditTaskViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectService _projectService;
        private readonly TeamService _teamService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _title = "";
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        private TaskType _selectedType;
        public TaskType SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
                UpdateGitHubBranchHint(); // Только меняем подсказку
            }
        }

        private MemberDto _selectedAssignee;
        public MemberDto SelectedAssignee
        {
            get => _selectedAssignee;
            set { _selectedAssignee = value; OnPropertyChanged(); }
        }

        private DateTime? _deadline;
        public DateTime? Deadline
        {
            get => _deadline;
            set { _deadline = value; OnPropertyChanged(); }
        }

        private string _githubBranchName = "";
        public string GithubBranchName
        {
            get => _githubBranchName;
            set { _githubBranchName = value; OnPropertyChanged(); }
        }

        private string _githubBranchHint = "feature/";
        public string GithubBranchHint
        {
            get => _githubBranchHint;
            set { _githubBranchHint = value; OnPropertyChanged(); }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasErrorMessage)); }
        }

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsEditMode { get; private set; }
        public int TaskId { get; private set; }
        public int ProjectId { get; private set; }

        public ObservableCollection<TaskType> TaskTypes { get; set; } = new();
        public ObservableCollection<MemberDto> TeamMembers { get; set; } = new();
        public ObservableCollection<MemberDto> AvailableAssignees { get; set; } = new();

        public string PageTitle => IsEditMode ? "Редактирование задачи" : "Создание задачи";
        public string SaveButtonText => IsEditMode ? "Сохранить изменения" : "Создать задачу";

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        private bool _isEditMode = false;

        public EditTaskViewModel()
        {
            _navigationService = App.NavigationService;
            _projectService = new ProjectService();
            _teamService = new TeamService();

            SaveCommand = new RelayCommand(async () => await SaveTaskAsync());
            CancelCommand = new RelayCommand(() => Cancel());
        }

        public async void InitializeForCreate(int projectId)
        {
            ProjectId = projectId;
            _isEditMode = false;

            await LoadTaskTypesAsync();
            await LoadTeamMembersAsync();

            // Устанавливаем дефолтные значения
            Deadline = DateTime.Today.AddDays(7);
            if (TaskTypes.Any())
                SelectedType = TaskTypes.First();
        }

        public async void InitializeForEdit(int taskId)
        {
            TaskId = taskId;
            _isEditMode = true;

            await LoadTaskTypesAsync();
            await LoadTeamMembersAsync();
            await LoadTaskDataAsync(taskId);
        }

        private async Task LoadTaskTypesAsync()
        {
            var types = await _projectService.GetTaskTypesAsync();
            TaskTypes.Clear();
            foreach (var type in types)
            {
                TaskTypes.Add(type);
            }
        }

        private async Task LoadTeamMembersAsync()
        {
            var team = await _teamService.GetCurrentTeamAsync();
            if (team != null)
            {
                TeamMembers.Clear();
                AvailableAssignees.Clear();

                foreach (var member in team.Members)
                {
                    TeamMembers.Add(member);
                    // В доступные исполнители добавляем всех, кроме капитана
                    if (!member.IsCaptain)
                    {
                        AvailableAssignees.Add(member);
                    }
                }

                // Если нет доступных исполнителей, показываем сообщение
                if (!AvailableAssignees.Any())
                {
                    ErrorMessage = "В команде нет участников, которым можно назначить задачу";
                }
            }
        }

        private async Task LoadTaskDataAsync(int taskId)
        {
            var task = await _projectService.GetTaskDetailsAsync(taskId);
            if (task != null)
            {
                Title = task.Title;
                Description = task.Description ?? "";
                SelectedType = TaskTypes.FirstOrDefault(t => t.Id == task.TypeId);
                SelectedAssignee = TeamMembers.FirstOrDefault(m => m.Id == task.AssignedToId);
                Deadline = task.Deadline;
                GithubBranchName = task.GithubBranchName ?? "";

                // Обновляем подсказку для существующей задачи
                UpdateGitHubBranchHint();
            }
        }

        private void UpdateGitHubBranchHint()
        {
            if (SelectedType == null) return;

            // Определяем префикс ветки в зависимости от типа задачи
            switch (SelectedType.Name.ToLower())
            {
                case "баг":
                case "bug":
                    GithubBranchHint = "bugfix/";
                    break;
                case "документация":
                case "documentation":
                    GithubBranchHint = "docs/";
                    break;
                case "фича":
                case "feature":
                default:
                    GithubBranchHint = "feature/";
                    break;
            }
        }

        private async Task SaveTaskAsync()
        {
            if (!ValidateForm())
                return;

            try
            {
                var dto = new CreateTaskDto
                {
                    Title = Title.Trim(),
                    Description = Description?.Trim(),
                    TypeId = SelectedType?.Id ?? 1,
                    AssignedToId = SelectedAssignee?.Id,
                    Deadline = Deadline,
                    GithubBranchName = GithubBranchName?.Trim()
                };

                if (_isEditMode)
                {
                    var result = await _projectService.UpdateTaskAsync(TaskId, dto);
                    MessageBox.Show(result.Message);
                    if (result.Success)
                    {
                        _navigationService.GoBack();
                    }
                }
                else
                {
                    var result = await _projectService.CreateTaskAsync(ProjectId, dto);
                    MessageBox.Show(result.Message);
                    if (result.Success)
                    {
                        _navigationService.GoBack();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка при сохранении: {ex.Message}";
            }
        }

        private void Cancel()
        {
            _navigationService.GoBack();
        }

        private bool ValidateForm()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "Название задачи обязательно для заполнения";
                return false;
            }

            if (SelectedType == null)
            {
                ErrorMessage = "Выберите тип задачи";
                return false;
            }

            if (Title.Length > 200)
            {
                ErrorMessage = "Название задачи не должно превышать 200 символов";
                return false;
            }

            if (Description?.Length > 1000)
            {
                ErrorMessage = "Описание задачи не должно превышать 1000 символов";
                return false;
            }

            // Валидация названия GitHub ветки
            if (!string.IsNullOrWhiteSpace(GithubBranchName))
            {
                if (!IsValidBranchName(GithubBranchName))
                {
                    ErrorMessage = "Название ветки может содержать только буквы, цифры, дефисы, подчеркивания и слеши";
                    return false;
                }
            }

            return true;
        }

        private bool IsValidBranchName(string branchName)
        {
            // GitHub branch name validation
            return System.Text.RegularExpressions.Regex.IsMatch(branchName, @"^[a-zA-Z0-9._\/-]+$");
        }
    }
}