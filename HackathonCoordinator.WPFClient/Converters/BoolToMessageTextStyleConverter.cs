using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToMessageTextStyleConverter : IValueConverter
    {
        public static BoolToMessageTextStyleConverter Instance { get; } = new BoolToMessageTextStyleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                return Application.Current.FindResource(
                   isMyMessage ? "SidebarTextColor" : "TextColor");
            }
            return Application.Current.FindResource("TextColor");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}