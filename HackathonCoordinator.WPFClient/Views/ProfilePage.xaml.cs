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