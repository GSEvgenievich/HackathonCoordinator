using HackathonCoordinator.WebAPI.Models;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex(@"^[a-zA-Z0-9._-]*$");
            var textBox = (TextBox)sender;
            var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            if (!regex.IsMatch(newText))
            {
                e.Handled = true;
                System.Media.SystemSounds.Beep.Play();
            }
        }
    }
}
