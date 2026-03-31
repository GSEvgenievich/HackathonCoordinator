using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class UsersManagementPage : Page
    {
        public UsersManagementPage()
        {
            InitializeComponent();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsersManagementViewModel viewModel)
            {
                if (viewModel.doDispose)
                    viewModel.Dispose();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsersManagementViewModel viewModel)
            {
                viewModel.doDispose = true;
            }
        }
    }
}