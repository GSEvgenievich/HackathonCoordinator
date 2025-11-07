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
    public class EditProjectViewModel : INotifyPropertyChanged
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectService _projectService;
        private readonly TeamService _teamService;

        private string _name = "";
        private string _description = "";
        private string _githubRepoName = "";
        private bool _createGitHubRepo = true;
        private string _errorMessage = "";
        private bool _isEditMode = false;
        private int _projectId = 0;
        private int _teamId = 0;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                // Автогенерация названия репозитория
                if (!_isEditMode && string.IsNullOrEmpty(GithubRepoName) && !string.IsNullOrEmpty(value))
                {
                    GithubRepoName = GenerateRepoName(value);
                }
            }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string GithubRepoName
        {
            get => _githubRepoName;
            set { _githubRepoName = value; OnPropertyChanged(); }
        }

        public bool CreateGitHubRepo
        {
            get => _createGitHubRepo;
            set { _createGitHubRepo = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasErrorMessage));
            }
        }

        public bool HasErrorMessage => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsEditMode => _isEditMode;
        public bool IsCreateMode => !_isEditMode;
        public bool CanEditGitHubRepo => !_isEditMode; // Только при создании можно редактировать

        public string PageTitle => _isEditMode ? "Редактирование проекта" : "Создание проекта";
        public string SaveButtonText => _isEditMode ? "Сохранить изменения" : "Создать проект";

        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public EditProjectViewModel()
        {
            _navigationService = App.NavigationService;
            _projectService = new ProjectService();
            _teamService = new TeamService();

            SaveCommand = new RelayCommand(async () => await SaveProjectAsync());
            CancelCommand = new RelayCommand(() => Cancel());
        }

        public void LoadProjectData(ProjectDto project)
        {
            _isEditMode = true;
            _projectId = project.Id;
            _teamId = project.TeamId;

            Name = project.Name;
            Description = project.Description ?? "";
            GithubRepoName = project.GithubRepoName ?? "";

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(CanEditGitHubRepo));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        public void InitializeForCreate(int teamId)
        {
            _teamId = teamId;
            _isEditMode = false;

            OnPropertyChanged(nameof(IsEditMode));
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(CanEditGitHubRepo));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        private async Task SaveProjectAsync()
        {
            if (!ValidateForm())
                return;

            try
            {
                if (_isEditMode)
                {
                    var dto = new UpdateProjectDto
                    {
                        Name = Name.Trim(),
                        Description = Description?.Trim()
                    };

                    var result = await _projectService.UpdateProjectAsync(_projectId, dto);
                    MessageBox.Show(result.Message);

                    if (result.Success)
                    {
                        _navigationService.NavigateTo(new TeamPage());
                    }
                }
                else
                {
                    var dto = new CreateProjectDto
                    {
                        Name = Name.Trim(),
                        Description = Description?.Trim(),
                        CreateGitHubRepo = CreateGitHubRepo,
                        GithubRepoName = CreateGitHubRepo ? GithubRepoName?.Trim() : null
                    };

                    var result = await _projectService.CreateProjectAsync(_teamId, dto);
                    MessageBox.Show(result.Message);

                    if (result.Success)
                    {
                        _navigationService.NavigateTo(new TeamPage());
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
            _navigationService.NavigateTo(new TeamPage());
        }

        private bool ValidateForm()
        {
            ErrorMessage = "";

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Название проекта обязательно для заполнения";
                return false;
            }

            if (Name.Length > 100)
            {
                ErrorMessage = "Название проекта не должно превышать 100 символов";
                return false;
            }

            if (Description?.Length > 500)
            {
                ErrorMessage = "Описание проекта не должно превышать 500 символов";
                return false;
            }

            // Валидация для создания проекта с GitHub
            if (!_isEditMode && CreateGitHubRepo)
            {
                if (string.IsNullOrWhiteSpace(GithubRepoName))
                {
                    ErrorMessage = "Название GitHub репозитория обязательно при создании репозитория";
                    return false;
                }

                if (!IsValidRepoName(GithubRepoName))
                {
                    ErrorMessage = "Название репозитория может содержать только буквы, цифры, дефисы и подчеркивания";
                    return false;
                }
            }

            return true;
        }

        private string GenerateRepoName(string projectName)
        {
            // Генерация безопасного имени для репозитория
            var repoName = projectName
                .ToLower()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Replace(".", "-");

            // Удаляем все недопустимые символы
            var invalidChars = System.Text.RegularExpressions.Regex.Replace(repoName, @"[^a-z0-9\-]", "");

            return invalidChars.Length > 0 ? invalidChars : "project-" + DateTime.Now.ToString("yyyyMMdd");
        }

        private bool IsValidRepoName(string repoName)
        {
            // GitHub repo name validation
            return System.Text.RegularExpressions.Regex.IsMatch(repoName, @"^[a-zA-Z0-9._-]+$");
        }
    }
}