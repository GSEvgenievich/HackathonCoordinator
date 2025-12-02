using HackathonCoordinator.ServiceLayer.Services;
using HackathonCoordinator.WPFClient.ViewModels;
using HackathonCoordinator.WPFClient.Views;
using System.Windows;
using System.Windows.Media.Animation;

namespace HackathonCoordinator.WPFClient
{
    public partial class MainWindow : Window
    {
        private bool _isExpanded = false;
        private readonly AuthService _authService;
        private readonly UserService _userService;

        public bool IsSidebarExpanded => _isExpanded;

        public MainWindow()
        {
            InitializeComponent();

            _authService = new AuthService();
            _userService = new UserService();
            Loaded += OnMainWindowLoadedAsync;
        }

        private async void OnMainWindowLoadedAsync(object sender, RoutedEventArgs e)
        {
            App.NavigationService.Initialize(MainFrame, SideBar);

            try
            {
                // Валидация токена при запуске
                var validation = await _authService.ValidateTokenAsync();

                if (!validation.Success)
                {
                    App.NavigationService.NavigateTo(new AuthorizationPage());
                    return;
                }

                // Получение информации о текущем пользователе
                var user = await _userService.GetCurrentUserAsync();

                if (!user.Success)
                {
                    App.NavigationService.NavigateTo(new AuthorizationPage());
                    return;
                }

                // Инициализация ViewModel
                if (DataContext is MainWindowViewModel viewModel)
                {
                    viewModel.InitializeNotificationsSignalR();
                    viewModel.CheckUserRole();
                    viewModel.GetUsername();
                }

                // Навигация в зависимости от наличия команды
                if (!string.IsNullOrEmpty(user.Data.TeamName))
                {
                    App.NavigationService.NavigateTo(new TeamPage());
                }
                else
                {
                    App.NavigationService.NavigateTo(new CompetitionsPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
                App.NavigationService.NavigateTo(new AuthorizationPage());
            }
        }

        private void SideBar_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isExpanded) return;

            _isExpanded = true;
            AnimateSidebar(200);
            SetTextVisibility(Visibility.Visible);
        }

        private void SideBar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isExpanded) return;

            _isExpanded = false;
            AnimateSidebar(60);
            SetTextVisibility(Visibility.Collapsed);
        }

        private void AnimateSidebar(double newWidth)
        {
            var animation = new DoubleAnimation
            {
                To = newWidth,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            SideBar.BeginAnimation(WidthProperty, animation);
        }

        private void SetTextVisibility(Visibility visibility)
        {
            ProfileText.Visibility = visibility;
            MainText.Visibility = visibility;
            UsersManagementText.Visibility = visibility;
            NotificationsText.Visibility = visibility;
            ChatsText.Visibility = visibility;
            ThemeText.Visibility = visibility;
            LogoutText.Visibility = visibility;
        }
    }
}