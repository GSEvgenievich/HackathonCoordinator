using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace HackathonCoordinator.WPFClient.Converters
{
    public class Base64ToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            try
            {
                byte[] bytes = null;

                // Если пришла строка Base64
                if (value is string base64 && !string.IsNullOrEmpty(base64))
                {
                    bytes = System.Convert.FromBase64String(base64);
                }
                // Если пришел массив байтов
                else if (value is byte[] byteArray && byteArray.Length > 0)
                {
                    bytes = byteArray;
                }
                else
                {
                    return null;
                }

                using var ms = new MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}