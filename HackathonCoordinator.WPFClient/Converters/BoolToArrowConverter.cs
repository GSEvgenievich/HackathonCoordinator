using System;
using System.Globalization;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class BoolToArrowConverter : IValueConverter
    {
        public static BoolToArrowConverter Instance { get; } = new BoolToArrowConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool expanded && expanded ? "▲" : "▼";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}