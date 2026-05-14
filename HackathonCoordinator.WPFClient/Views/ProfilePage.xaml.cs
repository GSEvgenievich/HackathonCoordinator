using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class ProfilePage : Page
    {
        private int? _userId;

        public ProfilePage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        public ProfilePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            Loaded += OnPageLoaded;
        }

        public void UpdateAvatar(string imagePath)
        {
            MainAvatar.ImagePath = imagePath;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel viewModel)
            {
                viewModel.doDispose = true;
                await viewModel.InitializeAsync(_userId);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel viewModel)
            {
                if (viewModel.doDispose)
                    viewModel.Dispose();
            }
        }
    }
}