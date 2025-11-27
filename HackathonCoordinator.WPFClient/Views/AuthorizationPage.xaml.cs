using HackathonCoordinator.WPFClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HackathonCoordinator.WPFClient.Views
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationPage.xaml
    /// </summary>
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
                // Скрываем placeholder при фокусе ИЛИ если есть пароль
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
