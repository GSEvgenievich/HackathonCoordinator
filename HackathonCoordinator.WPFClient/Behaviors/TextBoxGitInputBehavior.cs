using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HackathonCoordinator.WPFClient.Behaviors
{
    public class TextBoxGitInputBehavior
    {
        public static readonly DependencyProperty AllowOnlyGitHubRepoCharsProperty =
            DependencyProperty.RegisterAttached(
                "AllowOnlyGitHubRepoChars",
                typeof(bool),
                typeof(TextBoxGitInputBehavior),
                new PropertyMetadata(false, OnAllowOnlyGitHubRepoCharsChanged));

        public static bool GetAllowOnlyGitHubRepoChars(DependencyObject obj)
        {
            return (bool)obj.GetValue(AllowOnlyGitHubRepoCharsProperty);
        }

        public static void SetAllowOnlyGitHubRepoChars(DependencyObject obj, bool value)
        {
            obj.SetValue(AllowOnlyGitHubRepoCharsProperty, value);
        }

        private static void OnAllowOnlyGitHubRepoCharsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.PreviewTextInput += TextBox_PreviewTextInput;
                    textBox.PreviewKeyDown += TextBox_PreviewKeyDown;

                    // Обработка вставки через Ctrl+V
                    DataObject.AddPastingHandler(textBox, OnPaste);
                }
                else
                {
                    textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                    textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                    DataObject.RemovePastingHandler(textBox, OnPaste);
                }
            }
        }

        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Регулярное выражение для допустимых символов GitHub репозитория
            var regex = new Regex(@"^[a-zA-Z0-9._-]*$");

            // Проверяем весь текст после ввода
            var textBox = (TextBox)sender;
            var newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);

            if (!regex.IsMatch(newText))
            {
                e.Handled = true;
            }
        }

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left ||
                e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End ||
                e.Key == Key.Tab || e.Key == Key.Enter)
            {
                return;
            }

            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control &&
                (e.Key == Key.A || e.Key == Key.C || e.Key == Key.X || e.Key == Key.V || e.Key == Key.Z))
            {
                return;
            }

            if (!IsAllowedKey(e.Key))
            {
                e.Handled = true;
            }
        }

        private static bool IsAllowedKey(Key key)
        {
            // Буквы A-Z, a-z
            if (key >= Key.A && key <= Key.Z) return true;

            // Цифры 0-9
            if (key >= Key.D0 && key <= Key.D9) return true;
            if (key >= Key.NumPad0 && key <= Key.NumPad9) return true;

            // Специальные символы: . - _
            if (key == Key.OemPeriod || key == Key.Decimal) return true;    // Точка
            if (key == Key.OemMinus || key == Key.Subtract) return true;    // Дефис
            if (key == Key.OemQuestion) return true;                        // Слеш (может быть подчеркиванием на некоторых раскладках)

            return false;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                var regex = new Regex(@"^[a-zA-Z0-9._-]*$");

                if (!regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}

