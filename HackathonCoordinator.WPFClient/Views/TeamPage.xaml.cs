using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{

    /// <summary>
    /// Логика взаимодействия для TeamPage.xaml
    /// </summary>
    public partial class TeamPage : Page
    {
        private int? TeamId { get; set; } = null;

        public TeamPage(int? teamId)
        {
            InitializeComponent();
            TeamId = teamId;
            Loaded += OnPageLoaded;
        }

        public TeamPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private async void OnPageLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is TeamViewModel viewModel)
            {
                await viewModel.LoadTeamDataAsync(TeamId);
            }
        }
    }
}
