using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class TaskDetailsPage : Page
    {
        public TaskDetailsPage(int taskId)
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                if (DataContext is TaskDetailsViewModel viewModel)
                {
                    viewModel.LoadTaskData(taskId);
                }
            };
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TaskDetailsViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}