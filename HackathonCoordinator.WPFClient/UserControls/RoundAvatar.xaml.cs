using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HackathonCoordinator.WPFClient.UserControls
{
    /// <summary>
    /// Логика взаимодействия для RoundAvatar.xaml
    /// </summary>
    public partial class RoundAvatar : UserControl
    {
        public RoundAvatar()
        {
            InitializeComponent();
        }

        // Размер аватара (иконки)
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(double), typeof(RoundAvatar),
                new PropertyMetadata(50.0, OnSizeChanged));

        // Источник картинки
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(RoundAvatar));

        // Свойство для обводки (опционально, по дефолту нет)
        public static readonly DependencyProperty BorderBrushProperty =
            DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(RoundAvatar));

        public static readonly DependencyProperty BorderThicknessProperty =
            DependencyProperty.Register(nameof(BorderThickness), typeof(Thickness), typeof(RoundAvatar),
                new PropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register(nameof(ImagePath), typeof(string), typeof(RoundAvatar),
        new PropertyMetadata(null, OnImagePathChanged));

        // Public свойства
        public string ImagePath
        {
            get => (string)GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public ImageSource Source
        {
            get => (ImageSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Brush BorderBrush
        {
            get => (Brush)GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RoundAvatar avatar && e.NewValue is double newSize)
            {
                // Автоматически подстраиваем CornerRadius под размер
                avatar.MainBorder.CornerRadius = new CornerRadius(newSize / 2);
                avatar.Width = newSize;
                avatar.Height = newSize;
            }
        }

        private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RoundAvatar avatar && e.NewValue is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                    var bitmap = new BitmapImage(uri);
                    avatar.Source = bitmap;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }
    }
}
