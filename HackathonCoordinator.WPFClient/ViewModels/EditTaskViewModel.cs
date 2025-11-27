using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public class EditTaskViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly TeamService _teamService;
        private readonly TaskService _taskService;

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

        private TaskTypeDto _selectedType;
        public TaskTypeDto SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged();
                UpdateGitHubBranchHint();
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
        public string GitHubBranchName
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

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set { _isEditMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageTitle)); OnPropertyChanged(nameof(SaveButtonText)); }
        }

        private bool _hasGitHubRepo = false;
        public bool HasGitHubRepo
        {
            get => _hasGitHubRepo;
            set
            {
                _hasGitHubRepo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowGitHubSection));
            }
        }

        private bool _isBranchReadOnly = false;
        public bool IsBranchReadOnly
        {
            get => _isBranchReadOnly;
            set { _isBranchReadOnly = value; OnPropertyChanged(); }
        }

        private string _branchToolTip = "";
        public string BranchToolTip
        {
            get => _branchToolTip;
            set { _branchToolTip = value; OnPropertyChanged(); }
        }

        private bool _hasExistingBranch = false;
        public bool HasExistingBranch
        {
            get => _hasExistingBranch;
            set
            {
                _hasExistingBranch = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowBranchWarning));
                OnPropertyChanged(nameof(ShowBranchInfo));
                UpdateBranchReadOnlyState();
            }
        }

        // Свойства для управления видимостью элементов UI
        public bool ShowGitHubSection => HasGitHubRepo;
        public bool ShowBranchWarning => HasExistingBranch && IsEditMode;
        public bool ShowBranchInfo => !HasExistingBranch && !string.IsNullOrEmpty(GitHubBranchName) && HasGitHubRepo;

        public int TaskId { get; private set; }
        public int ProjectId { get; private set; }

        public ObservableCollection<TaskTypeDto> TaskTypes { get; set; } = new();
        public ObservableCollection<MemberDto> TeamMembers { get; set; } = new();

        public string PageTitle => IsEditMode ? "Редактирование задачи" : "Создание задачи";
        public string PageSubtitle => IsEditMode ? "Редактирование существующей задачи" : "Создание новой задачи для команды";
        public string SaveButtonText => IsEditMode ? "Сохранить изменения" : "Создать задачу";

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public EditTaskViewModel()
        {
            _navigationService = App.NavigationService;
            _teamService = new TeamService();
            _taskService = new TaskService();

            SaveCommand = new RelayCommand(async () => await SaveTaskAsync());
            CancelCommand = new RelayCommand(() => Cancel());
        }

        public async void InitializeForCreate(int projectId)
        {
            ProjectId = projectId;
            IsEditMode = false;

            await LoadTeamDataAsync();
            await LoadTaskTypesAsync();
            await LoadTeamMembersAsync();

            // Устанавливаем дефолтные значения
            Deadline = DateTime.Today.AddDays(7);
            if (TaskTypes.Any())
                SelectedType = TaskTypes.First();

            // Для новой задачи ветка всегда редактируема
            IsBranchReadOnly = false;
            BranchToolTip = "Введите название для новой ветки GitHub";
        }

        public async void InitializeForEdit(int taskId)
        {
            TaskId = taskId;
            IsEditMode = true;

            await LoadTeamDataAsync();
            await LoadTaskTypesAsync();
            await LoadTeamMembersAsync();
            await LoadTaskDataAsync(taskId);
        }

        private async Task LoadTeamDataAsync()
        {
            var team = await _teamService.GetCurrentTeamAsync();
            if (team != null)
            {
                HasGitHubRepo = !string.IsNullOrEmpty(team.GitHubUrl) && team.GitHubUrl != "Не указан";
            }
        }

        private async Task LoadTaskTypesAsync()
        {
            var types = await _taskService.GetTaskTypesAsync();
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

                foreach (var member in team.Members)
                {
                    TeamMembers.Add(member);
                }

                if (!TeamMembers.Any())
                {
                    ErrorMessage = "В команде нет участников, которым можно назначить задачу";
                }
            }
        }

        private async Task LoadTaskDataAsync(int taskId)
        {
            var task = await _taskService.GetTaskDetailsAsync(taskId);
            if (task != null)
            {
                Title = task.Title;
                Description = task.Description ?? "";
                SelectedType = TaskTypes.FirstOrDefault(t => t.Id == task.TypeId);
                SelectedAssignee = TeamMembers.FirstOrDefault(m => m.Id == task.AssignedToId);
                Deadline = task.Deadline;
                GitHubBranchName = task.GitHubBranchName ?? "";

                // Определяем, есть ли уже ветка в GitHub
                HasExistingBranch = !string.IsNullOrEmpty(task.GitHubBranchName);

                // Обновляем подсказку и состояние поля
                UpdateGitHubBranchHint();
                UpdateBranchReadOnlyState();
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

        private void UpdateBranchReadOnlyState()
        {
            if (IsEditMode && HasExistingBranch)
            {
                IsBranchReadOnly = true;
                BranchToolTip = "Ветка уже создана в GitHub. Изменить название нельзя.";
            }
            else if (HasGitHubRepo)
            {
                IsBranchReadOnly = false;
                BranchToolTip = "Введите название для новой ветки GitHub";
            }
            else
            {
                IsBranchReadOnly = true;
                BranchToolTip = "Для создания ветки необходимо подключить GitHub репозиторий к команде";
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
                    // Передаем название ветки только если оно указано И команда имеет GitHub репозиторий
                    GitHubBranchName = HasGitHubRepo && !string.IsNullOrWhiteSpace(GitHubBranchName)
                        ? GitHubBranchName.Trim()
                        : null
                };

                if (IsEditMode)
                {
                    var result = await _taskService.UpdateTaskAsync(TaskId, dto);

                    if (result.Success)
                    {
                        var message = result.Message;

                        if (!string.IsNullOrWhiteSpace(dto.GitHubBranchName) && !HasExistingBranch)
                        {
                            if (!message.Contains("ветк"))
                            {
                                message += $"\n\n✅ Ветка '{GitHubBranchName}' создана в GitHub репозитории";
                            }
                        }

                        MessageBox.Show(message);
                        _navigationService.GoBack();
                    }
                    else
                    {
                        MessageBox.Show(result.Message);
                    }

                }
                else
                {
                    var result = await _taskService.CreateTaskAsync(ProjectId, dto);

                    if (result.Success)
                    {
                        var message = result.Message;

                        if (!string.IsNullOrWhiteSpace(dto.GitHubBranchName))
                        {
                            if (!message.Contains("ветк"))
                            {
                                message += $"\n\nВетка '{GitHubBranchName}' создана в GitHub репозитории команды";
                            }
                        }

                        MessageBox.Show(message);
                        _navigationService.GoBack();
                    }
                    else
                    {
                        MessageBox.Show(result.Message);
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

            // Валидация названия GitHub ветки только если она указана и команда имеет репозиторий
            if (HasGitHubRepo && !string.IsNullOrWhiteSpace(GitHubBranchName))
            {
                if (!IsValidBranchName(GitHubBranchName))
                {
                    ErrorMessage = "Название ветки может содержать только буквы, цифры, дефисы, подчеркивания и слеши";
                    return false;
                }

                // Проверяем, что ветка не начинается или не заканчивается слешем
                if (GitHubBranchName.StartsWith("/") || GitHubBranchName.EndsWith("/"))
                {
                    ErrorMessage = "Название ветки не может начинаться или заканчиваться слешем";
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