using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        public ProfilePage(int userId)
        {
            InitializeComponent();
            Loaded += (s, e) => OnPageLoaded(userId);
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel viewModel)
            {
                await viewModel.LoadProfileAsync();
            }
        }

        private async void OnPageLoaded(int userId)
        {
            if (DataContext is ProfileViewModel viewModel)
            {
                await viewModel.LoadProfileAsync(userId);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ProfileViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}