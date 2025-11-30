using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToMessageStyleConverter : IValueConverter
    {
        public static BoolToMessageStyleConverter Instance { get; } = new BoolToMessageStyleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                return Application.Current.FindResource(
                    isMyMessage ? "MyMessageStyle" : "OtherMessageStyle");
            }
            return Application.Current.FindResource("OtherMessageStyle");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}