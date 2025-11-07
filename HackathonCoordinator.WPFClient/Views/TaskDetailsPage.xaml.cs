using HackathonCoordinator.WPFClient.ViewModels;
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
    }
}