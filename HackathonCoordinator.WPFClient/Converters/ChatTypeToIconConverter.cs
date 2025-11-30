using System;
using System.Globalization;
using System.Windows.Data;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class ChatTypeToIconConverter : IValueConverter
    {
        public static readonly ChatTypeToIconConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string chatType)
            {
                return chatType.ToLower() switch
                {
                    "team" => "👥",
                    "task" => "🎯",
                    _ => "💬"
                };
            }
            return "💬";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}