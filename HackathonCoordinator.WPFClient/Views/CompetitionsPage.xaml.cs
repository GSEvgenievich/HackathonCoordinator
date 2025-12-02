using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class CompetitionsPage : Page
    {
        public CompetitionsPage()
        {
            InitializeComponent();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Освобождение ресурсов при уходе со страницы
            if (DataContext is CompetitionsViewModel viewModel)
            {
                // Очистка коллекций для освобождения памяти
                viewModel.Competitions?.Clear();
                viewModel.FilteredCompetitions?.Clear();
                viewModel.StatusFilters?.Clear();
            }
        }
    }
}