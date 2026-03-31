using HackathonCoordinator.WPFClient.ViewModels;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace HackathonCoordinator.WPFClient.Views
{
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
         
        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TeamViewModel viewModel)
            {
                viewModel.doDispose = true;
                await viewModel.LoadTeamDataAsync(TeamId);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TeamViewModel viewModel)
            {
                if (viewModel.doDispose)
                    viewModel.Dispose();
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