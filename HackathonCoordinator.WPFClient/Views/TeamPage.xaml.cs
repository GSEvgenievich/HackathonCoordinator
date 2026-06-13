using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class TeamPage : Page
    {
        private readonly TeamDto _team;

        public TeamPage(TeamDto team)
        {
            InitializeComponent();
            _team = team;
            Loaded += OnPageLoaded;
        }
         
        private async void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TeamViewModel viewModel)
            {
                viewModel.doDispose = true;
                await viewModel.InitializeAsync(_team);
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