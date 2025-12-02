using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class CompetitionDetailsPage : Page
    {
        private static CompetitionDto _competition;

        public CompetitionDetailsPage(CompetitionDto competition)
        {
            InitializeComponent();
            _competition = competition;
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CompetitionDetailsViewModel viewModel && _competition != null)
            {
                viewModel.Competition = _competition;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CompetitionDetailsViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}