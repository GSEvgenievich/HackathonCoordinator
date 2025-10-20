using HackathonCoordinator.ServiceLayer.Services;
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
                bool isValid = await _authService.ValidateTokenAsync();

                if (!isValid)
                {
                    App.NavigationService.NavigateTo(new AuthorizationPage());
                    return;
                }

                var user = await _userService.GetCurrentUserAsync();

                if (user == null)
                {
                    App.NavigationService.NavigateTo(new AuthorizationPage());
                    return;
                }

                if (!string.IsNullOrEmpty(user.TeamName))
                {
                    App.NavigationService.NavigateTo(new TeamPage());
                }
                else
                {
                    App.NavigationService.NavigateTo(new NoTeamPage());
                }
            }
            catch (Exception ex)
            {
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
            MainText.Visibility = visibility;
            ProfileText.Visibility = visibility;
            NotificationsText.Visibility = visibility;
            ChatsText.Visibility = visibility;
        }
    }
}