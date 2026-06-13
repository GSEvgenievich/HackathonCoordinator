using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class TaskDetailsPage : Page
    {
        private readonly TaskDetailsDto _task;

        public TaskDetailsPage(TaskDetailsDto task)
        {
            InitializeComponent();
            _task = task;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TaskDetailsViewModel viewModel)
            {
                viewModel.doDispose = true;
                await viewModel.InitializeAsync(_task);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TaskDetailsViewModel viewModel)
            {
                if (viewModel.doDispose)
                    viewModel.Dispose();
            }
        }
    }
}