using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class CompetitionDetailsPage : Page
    {
        private readonly CompetitionDto _competition;

        public CompetitionDetailsPage(CompetitionDto competition)
        {
            InitializeComponent();
            _competition = competition;
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CompetitionDetailsViewModel viewModel)
            {
                viewModel.doDispose = true;
                await viewModel.InitializeAsync(_competition);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CompetitionDetailsViewModel viewModel)
            {
                if (viewModel.doDispose)
                    viewModel.Dispose();
            }
        }
    }
}