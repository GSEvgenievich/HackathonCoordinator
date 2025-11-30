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
    public class UsersManagementViewModel : INotifyPropertyChanged
    {
        private readonly UserService _userService;
        private readonly NavigationService _navigationService;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private ObservableCollection<UserDto> _allUsers = new();
        public ObservableCollection<UserDto> AllUsers
        {
            get => _allUsers;
            set { _allUsers = value; OnPropertyChanged(); }
        }

        private ObservableCollection<UserDto> _filteredUsers = new();
        public ObservableCollection<UserDto> FilteredUsers
        {
            get => _filteredUsers;
            set { _filteredUsers = value; OnPropertyChanged(); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilterUsers(); }
        }

        private UserDto _userToDelete;
        public UserDto UserToDelete
        {
            get => _userToDelete;
            set { _userToDelete = value; OnPropertyChanged(); }
        }

        private bool _showDeleteDialog;
        public bool ShowDeleteDialog
        {
            get => _showDeleteDialog;
            set { _showDeleteDialog = value; OnPropertyChanged(); }
        }

        // Статистика
        public int TotalUsersCount => AllUsers.Count;
        public int UsersInTeamsCount => AllUsers.Count(u => u.TeamId.HasValue);
        public int UsersWithoutTeamCount => AllUsers.Count(u => !u.TeamId.HasValue);
        public bool HasNoUsers => !FilteredUsers.Any();

        // Фильтры
        public ObservableCollection<UserListStatusFilter> StatusFilters { get; } = new()
        {
            new UserListStatusFilter { Id = 0, Name = "Все участники" },
            new UserListStatusFilter { Id = 1, Name = "В командах" },
            new UserListStatusFilter { Id = 2, Name = "Без команды" },
            new UserListStatusFilter { Id = 3, Name = "С GitHub" },
            new UserListStatusFilter { Id = 4, Name = "Капитаны" }
        };

        private UserListStatusFilter _selectedStatusFilter;
        public UserListStatusFilter SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set { _selectedStatusFilter = value; OnPropertyChanged(); FilterUsers(); }
        }

        public ICommand BackCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand ViewUserCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand ConfirmDeleteCommand { get; }
        public ICommand CancelDeleteCommand { get; }

        public UsersManagementViewModel()
        {
            _userService = new UserService();
            _navigationService = App.NavigationService;

            BackCommand = new RelayCommand(() => _navigationService.GoBack());
            DeleteUserCommand = new RelayCommand<UserDto>(user => ShowDeleteConfirmation(user));
            ViewUserCommand = new RelayCommand<UserDto>(user => ViewUserDetails(user));
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            ConfirmDeleteCommand = new RelayCommand(async () => await ExecuteDeleteUserAsync());
            CancelDeleteCommand = new RelayCommand(() => CancelDelete());

            SelectedStatusFilter = StatusFilters[0];
            LoadUsersAsync();
        }

        private async void LoadUsersAsync()
        {
            try
            {
                var result = await _userService.GetAllUsersAsync();
                if (result.Success)
                {
                    AllUsers = new ObservableCollection<UserDto>(result.Data);
                    FilterUsers();
                    UpdateStatistics();
                }
                else
                {
                    MessageBox.Show("Ошибка загрузки пользователей: " + result.Message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void FilterUsers()
        {
            if (AllUsers == null) return;

            var filtered = AllUsers.AsEnumerable();

            // Поиск
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(u =>
                    u.Username.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower));
            }

            // Фильтр по статусу
            if (SelectedStatusFilter != null)
            {
                filtered = SelectedStatusFilter.Id switch
                {
                    1 => filtered.Where(u => u.TeamId.HasValue), // В командах
                    2 => filtered.Where(u => !u.TeamId.HasValue), // Без команды
                    3 => filtered.Where(u => !string.IsNullOrEmpty(u.GitHubUsername)), // С GitHub
                    4 => filtered.Where(u => u.RoleId == 1), // Капитаны
                    _ => filtered // Все
                };
            }

            FilteredUsers = new ObservableCollection<UserDto>(filtered);
            OnPropertyChanged(nameof(HasNoUsers));
        }

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalUsersCount));
            OnPropertyChanged(nameof(UsersInTeamsCount));
            OnPropertyChanged(nameof(UsersWithoutTeamCount));
        }

        private void ResetFilters()
        {
            SearchText = "";
            SelectedStatusFilter = StatusFilters[0];
        }

        private void ShowDeleteConfirmation(UserDto user)
        {
            if (user == null) return;

            UserToDelete = user;
            ShowDeleteDialog = true;
        }

        private void CancelDelete()
        {
            ShowDeleteDialog = false;
            UserToDelete = null;
        }

        private async Task ExecuteDeleteUserAsync()
        {
            if (UserToDelete == null) return;

            try
            {
                var result = await _userService.DeleteUserAsync(UserToDelete.Id);
                MessageBox.Show(result.Message);

                if (result.Success)
                {
                    // Удаляем пользователя из списка
                    AllUsers.Remove(UserToDelete);
                    FilterUsers();
                    UpdateStatistics();
                }

                ShowDeleteDialog = false;
                UserToDelete = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }

        private void ViewUserDetails(UserDto user)
        {
            if (user != null)
            {
                MessageBox.Show($"Детали пользователя:\n\n" +
                              $"Имя: {user.Username}\n" +
                              $"Email: {user.Email}\n" +
                              $"Роль: {user.RoleName}\n" +
                              $"GitHub: {user.GitHubUsername ?? "Не привязан"}\n" +
                              $"Команда: {user.TeamName ?? "Не в команде"}",
                              "Информация о пользователе");
            }
        }
    }

    public class UserListStatusFilter
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}