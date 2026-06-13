using System.Globalization;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class DeadlineDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime deadline)
            {
                if (deadline.TimeOfDay == TimeSpan.Zero)
                    return deadline.ToString("dd.MM.yyyy");
                return deadline.ToString("dd.MM.yyyy HH:mm");
            }
            return "Не установлен";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}