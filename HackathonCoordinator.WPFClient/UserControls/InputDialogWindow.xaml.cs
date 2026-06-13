using System.Windows;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.UserControls
{
    public partial class InputDialogWindow : Window
    {
        private string _inputValue = "";

        public string InputValue => _inputValue;

        public InputDialogWindow(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();

            TitleText.Text = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;

            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _inputValue = InputTextBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
            }
        }

        public static string Show(string title, string prompt, string defaultValue = "")
        {
            var dialog = new InputDialogWindow(title, prompt, defaultValue);
            dialog.Owner = Application.Current.MainWindow;

            var result = dialog.ShowDialog();
            return result == true ? dialog.InputValue : null;
        }
    }
}