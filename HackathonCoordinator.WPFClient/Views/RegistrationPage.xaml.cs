using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class RegistrationPage : Page
    {
        public RegistrationPage()
        {
            InitializeComponent();

            // Обработчики для основного пароля
            PasswordBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is RegistrationViewModel vm)
                {
                    vm.Password = PasswordBox.Password;
                    UpdatePasswordPlaceholder();
                }
            };

            PasswordBox.GotFocus += (s, e) => UpdatePasswordPlaceholder();
            PasswordBox.LostFocus += (s, e) => UpdatePasswordPlaceholder();

            // Обработчики для подтверждения пароля
            ConfirmPasswordBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is RegistrationViewModel vm)
                {
                    vm.ConfirmPassword = ConfirmPasswordBox.Password;
                    UpdateConfirmPasswordPlaceholder();
                }
            };

            ConfirmPasswordBox.GotFocus += (s, e) => UpdateConfirmPasswordPlaceholder();
            ConfirmPasswordBox.LostFocus += (s, e) => UpdateConfirmPasswordPlaceholder();
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

        private void UpdateConfirmPasswordPlaceholder()
        {
            var placeholder = ConfirmPasswordBox.Template.FindName("placeholderText", ConfirmPasswordBox) as TextBlock;
            if (placeholder != null)
            {
                if (ConfirmPasswordBox.IsFocused || !string.IsNullOrEmpty(ConfirmPasswordBox.Password))
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