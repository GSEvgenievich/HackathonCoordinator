using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToMessageTextColorConverter : IValueConverter
    {
        public static BoolToMessageTextColorConverter Instance { get; } = new BoolToMessageTextColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMyMessage)
            {
                return isMyMessage ? Brushes.White : new SolidColorBrush(Color.FromRgb(33, 33, 33));
            }
            return new SolidColorBrush(Color.FromRgb(33, 33, 33));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}