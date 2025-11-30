using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.ServiceLayer.Storages;
using HackathonCoordinator.WPFClient.Helpers;
using HackathonCoordinator.WPFClient.Services;
using HackathonCoordinator.WPFClient.Views;
using Microsoft.AspNetCore.SignalR.Client;
using Org.BouncyCastle.Bcpg;
using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly TeamService _teamService;
        private readonly UserService _userService;
        private readonly AuthService _authService;
        private HubConnection _notificationHubConnection;

        private readonly string[] _themes = { "Light", "Dark", "Summer", "Spring", "Winter", "Autumn" };
        private int _currentThemeIndex = 0;

        private bool _isOrganizer = false;
        public bool IsOrganizer
        {
            get => _isOrganizer;
            set => SetProperty(ref _isOrganizer, value);
        }
        private bool _notificationHubConnected = false;
        private int _unreadNotificationsCount;
        public int UnreadNotificationsCount
        {
            get => _unreadNotificationsCount;
            set
            {
                SetProperty(ref _unreadNotificationsCount, value);
                OnPropertyChanged(nameof(UnreadNotificationsCount));
                OnPropertyChanged(nameof(HasUnreadNotifications));
                OnPropertyChanged(nameof(NotificationsButtonText));
            }
        }

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }
        public bool HasUnreadNotifications => UnreadNotificationsCount > 0;
        public string NotificationsButtonText => HasUnreadNotifications ?
            $"🔔 ({UnreadNotificationsCount})" : "🔔";
        public string CurrentThemeName => _themes[_currentThemeIndex];

        public ICommand OpenProfileCommand { get; }
        public ICommand OpenMainPageCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand OpenUsersManagementCommand { get; }
        public ICommand OpenChatsCommand {  get; }
        public ICommand OpenNotificationsCommand { get; }

        public MainWindowViewModel()
        {
            _navigationService = App.NavigationService;
            _teamService = new TeamService();
            _userService = new UserService();
            _authService = new AuthService();

            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            OpenNotificationsCommand = new RelayCommand(() => _navigationService.NavigateTo(new NotificationsPage()));
            OpenMainPageCommand = new RelayCommand(OpenMainPage);
            OpenChatsCommand = new RelayCommand(() => _navigationService.NavigateTo(new ChatsPage()));
            OpenProfileCommand = new RelayCommand(() => _navigationService.NavigateTo(new ProfilePage()));
            LogoutCommand = new RelayCommand(ExecuteLogout);
            OpenUsersManagementCommand = new RelayCommand(() => ExecuteOpenUsersManagement());

            LoadUnreadNotificationsCount();
            InitializeNotificationsSignalR();

            Application.Current.Exit += (s, e) => DisposeNotificationHub();
        }

        private async void LoadUnreadNotificationsCount()
        {
            try
            {
                var notificationService = new NotificationService();
                var response = await notificationService.GetUnreadCountAsync();
                if (response.Success)
                {
                    UnreadNotificationsCount = response.Data;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки счетчика уведомлений: {ex.Message}");
            }
        }

        private async void InitializeNotificationsSignalR()
        {
            var baseUrl = "http://localhost:5046";

            _notificationHubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/notificationhub", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(SecureTokenStorage.GetToken());
                })
                .WithAutomaticReconnect(new[] {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(10)
                })
                .Build();

            SetupNotificationHubEvents();

            try
            {
                await _notificationHubConnection.StartAsync();
                _notificationHubConnected = true;

                System.Diagnostics.Debug.WriteLine("Успешно подключен к хабу уведомлений");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подключения к уведомлениям: {ex.Message}");
            }
        }

        private void SetupNotificationHubEvents()
        {
            _notificationHubConnection.Closed += async (error) =>
            {
                _notificationHubConnected = false;
                System.Diagnostics.Debug.WriteLine("Соединение с уведомлениями разорвано");

                // Пытаемся переподключиться через 5 секунд
                await Task.Delay(5000);
                InitializeNotificationsSignalR();
            };

            _notificationHubConnection.Reconnected += (connectionId) =>
            {
                _notificationHubConnected = true;
                System.Diagnostics.Debug.WriteLine("Переподключен к уведомлениям");
                return Task.CompletedTask;
            };

            // Получение обновления счетчика непрочитанных
            _notificationHubConnection.On<int>("UpdateUnreadCount", (unreadCount) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UnreadNotificationsCount = unreadCount;
                });
            });

            // Получение нового уведомления
            _notificationHubConnection.On<object>("ReceiveNotification", (notification) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UnreadNotificationsCount++;
                });
            });
        }

        public async void SubscribeToNotifications(bool isOrganizer, int? teamId)
        {
            await _notificationHubConnection.InvokeAsync("SubscribeToUserNotifications");

            if(isOrganizer)
                await _notificationHubConnection.InvokeAsync("SubscribeToOrganizersNotifications");

            if (teamId != null)
                await _notificationHubConnection.InvokeAsync("SubscribeToTeamNotifications", teamId);
        }

        public async void UnSubscribeFromNotifications()
        {
            var user = await _userService.GetCurrentUserAsync();

            await _notificationHubConnection.InvokeAsync("UnsubscribeFromAllNotifications", user.Data.TeamId);
        }

        public async void DisposeNotificationHub()
        {
            if (_notificationHubConnection != null)
            {
                try
                {
                    await _notificationHubConnection.DisposeAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии соединения: {ex.Message}");
                }
            }
        }

        public async void CheckUserRole()
        {
            var user = await _userService.GetCurrentUserAsync();
            IsOrganizer = user.Data.RoleId == 3; 
            OnPropertyChanged(nameof(IsOrganizer));
        }

        private void ToggleTheme()
        {
            _currentThemeIndex = (_currentThemeIndex + 1) % _themes.Length;
            App.SwitchTheme(_themes[_currentThemeIndex]);
            OnPropertyChanged(nameof(CurrentThemeName));
        }

        private void ExecuteOpenUsersManagement()
        {
            App.NavigationService.NavigateTo(new UsersManagementPage());
        }

        public async void GetUsername()
        {
            try
            {
                var user = await _userService.GetCurrentUserAsync();
                Username = user.Data.Username ?? "Гость";
            }
            catch
            {
                Username = "Гость";
            }
        }

        private async void OpenMainPage()
        {
            var teamId = await _teamService.GetCurrentTeamIdAsync();

            if (!teamId.Success)
                _navigationService.NavigateTo(new CompetitionsPage());
            else
                _navigationService.NavigateTo(new TeamPage());
        }

        private void ExecuteLogout()
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Подтверждение выхода",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                UnSubscribeFromNotifications();

                _authService.Logout();
                _navigationService.NavigateTo(new AuthorizationPage());
            }
        }
    }
}