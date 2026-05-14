using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class ImagePreviewWindow : Window
    {
        private const double MIN_ZOOM = 0.1;
        private const double MAX_ZOOM = 5.0;
        private const double ZOOM_STEP = 0.1;
        private const double DEFAULT_ZOOM = 1.0;

        public byte[] ImageBytes { get; }
        public string FileName { get; }
        public string FormattedSize => FormatSize(ImageBytes.Length);

        private bool _zoomIndicatorVisible;
        public bool ZoomIndicatorVisible
        {
            get => _zoomIndicatorVisible;
            set
            {
                _zoomIndicatorVisible = value;
                OnPropertyChanged(nameof(ZoomIndicatorVisible));
            }
        }

        public ImagePreviewWindow(byte[] imageBytes, string fileName)
        {
            InitializeComponent();
            ImageBytes = imageBytes;
            FileName = fileName;
            DataContext = this;

            LoadImage();
        }

        private void LoadImage()
        {
            if (ImageBytes == null || ImageBytes.Length == 0)
            {
                Close();
                return;
            }

            try
            {
                using var ms = new MemoryStream(ImageBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                PreviewImage.Source = bitmap;
                Title = $"Просмотр - {FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private string FormatSize(long size)
        {
            return size switch
            {
                < 1024 => $"{size} B",
                < 1024 * 1024 => $"{size / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{size / (1024.0 * 1024):F1} MB",
                _ => $"{size / (1024.0 * 1024 * 1024):F1} GB"
            };
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            CenterImage();
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Получаем позицию мыши относительно изображения
                var mousePosition = e.GetPosition(PreviewImage);

                // Получаем текущий масштаб
                var currentScale = ImageScaleTransform.ScaleX;

                // Вычисляем новый масштаб
                var delta = e.Delta > 0 ? ZOOM_STEP : -ZOOM_STEP;
                var newScale = currentScale + delta;
                newScale = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, newScale));

                if (Math.Abs(newScale - currentScale) > 0.001)
                {
                    // Вычисляем смещение для масштабирования к курсору
                    var relativeX = mousePosition.X / PreviewImage.ActualWidth;
                    var relativeY = mousePosition.Y / PreviewImage.ActualHeight;

                    // Применяем новый масштаб
                    ImageScaleTransform.ScaleX = newScale;
                    ImageScaleTransform.ScaleY = newScale;

                    // Корректируем позицию скролла, чтобы масштабирование было к курсору
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var newHorizontalOffset = (relativeX * MainScrollViewer.ScrollableWidth) - (MainScrollViewer.ViewportWidth * relativeX);
                        var newVerticalOffset = (relativeY * MainScrollViewer.ScrollableHeight) - (MainScrollViewer.ViewportHeight * relativeY);

                        MainScrollViewer.ScrollToHorizontalOffset(Math.Max(0, newHorizontalOffset));
                        MainScrollViewer.ScrollToVerticalOffset(Math.Max(0, newVerticalOffset));
                    }));

                    UpdateZoomIndicator();
                }

                e.Handled = true;
            }
        }

        private void UpdateZoomIndicator()
        {
            var zoomPercent = (int)(ImageScaleTransform.ScaleX * 100);
            ZoomIndicatorText.Text = $"{zoomPercent}%";

            ZoomIndicatorVisible = true;

            var timer = new System.Timers.Timer(800);
            timer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ZoomIndicatorVisible = false;
                });
                timer.Stop();
                timer.Dispose();
            };
            timer.Start();
        }

        private void CenterImage()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (MainScrollViewer != null && PreviewImage.Source != null)
                {
                    MainScrollViewer.ScrollToHorizontalOffset((MainScrollViewer.ScrollableWidth) / 2);
                    MainScrollViewer.ScrollToVerticalOffset((MainScrollViewer.ScrollableHeight) / 2);
                }
            }));
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            ImageScaleTransform.ScaleX = DEFAULT_ZOOM;
            ImageScaleTransform.ScaleY = DEFAULT_ZOOM;
            UpdateZoomIndicator();
            CenterImage();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = FileName,
                    Filter = "Изображения|*.jpg;*.jpeg;*.png;*.gif|Все файлы|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(saveFileDialog.FileName, ImageBytes);
                    MessageBox.Show($"Файл сохранен:\n{saveFileDialog.FileName}", "Успешно",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Добавьте метод для INotifyPropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}