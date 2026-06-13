using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class AuthorizationPage : Page
    {
        public AuthorizationPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;

            PasswordBox.PasswordChanged += OnPasswordChanged;
            PasswordBox.GotFocus += OnPasswordFocusChanged;
            PasswordBox.LostFocus += OnPasswordFocusChanged;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthorizationViewModel vm)
            {
                vm.Password = PasswordBox.Password;
                UpdatePasswordPlaceholder();
            }
        }

        private void OnPasswordFocusChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            var placeholder = PasswordBox.Template?.FindName("placeholderText", PasswordBox) as TextBlock;
            if (placeholder != null)
            {
                placeholder.Visibility = (PasswordBox.IsFocused || !string.IsNullOrEmpty(PasswordBox.Password))
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }
    }
}