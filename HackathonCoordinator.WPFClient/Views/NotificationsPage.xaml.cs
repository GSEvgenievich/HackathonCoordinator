using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class NotificationsPage : Page
    {
        private bool _isInitialized = false;

        public NotificationsPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized) return;

            if (DataContext is NotificationsViewModel viewModel)
            {
                viewModel.doDispose = true;
                await viewModel.InitializeAsync();
                _isInitialized = true;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is NotificationsViewModel viewModel)
            {
                if (viewModel.doDispose)
                    viewModel.Dispose();
            }
        }
    }
}