using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToMessageContainerStyleConverter : IValueConverter
    {
        public static BoolToMessageContainerStyleConverter Instance { get; } = new BoolToMessageContainerStyleConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                return Application.Current.FindResource(
                    isMyMessage ? "MyMessageContainerStyle" : "OtherMessageContainerStyle");
            }
            return Application.Current.FindResource("OtherMessageContainerStyle");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}