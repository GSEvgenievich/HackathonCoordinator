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

            PasswordBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is AuthorizationViewModel vm)
                {
                    vm.Password = PasswordBox.Password;
                    UpdatePasswordPlaceholder();
                }
            };

            PasswordBox.GotFocus += (s, e) => UpdatePasswordPlaceholder();
            PasswordBox.LostFocus += (s, e) => UpdatePasswordPlaceholder();
            Loaded += (s, e) => UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            var placeholder = PasswordBox.Template.FindName("placeholderText", PasswordBox) as TextBlock;
            if (placeholder != null)
            {
                if (PasswordBox.IsFocused || !string.IsNullOrEmpty(PasswordBox.Password))
                {
                    placeholder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    placeholder.Visibility = Visibility.Visible;
                }
            }
        }
    }
}