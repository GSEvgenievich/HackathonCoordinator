using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class CompetitionStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string statusColor)
            {
                return statusColor switch
                {
                    "Green" => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                    "Orange" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    "Gray" => new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
                };
            }
            return new SolidColorBrush(Color.FromRgb(108, 117, 125));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
