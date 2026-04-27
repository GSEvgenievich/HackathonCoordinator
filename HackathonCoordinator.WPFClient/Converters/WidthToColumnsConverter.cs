using System.Globalization;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class WidthToColumnsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                if (width >= 1200) return 2;
                return 1;
            }
            return 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
