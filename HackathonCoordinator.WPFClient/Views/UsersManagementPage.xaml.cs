using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class UsersManagementPage : Page
    {
        private bool _isInitialized = false;

        public UsersManagementPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsersManagementViewModel viewModel)
            {
                viewModel.doDispose = true;
                await viewModel.InitializeAsync();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsersManagementViewModel viewModel)
            {
                if (viewModel.doDispose) viewModel.Dispose();
            }
        }
    }
}