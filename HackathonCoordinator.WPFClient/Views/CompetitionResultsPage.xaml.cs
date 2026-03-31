using HackathonCoordinator.ServiceLayer.DTOs;
using HackathonCoordinator.WPFClient.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace HackathonCoordinator.WPFClient.Views
{
    public partial class CompetitionResultsPage : Page
    {
        private Border _selectedBorder;
        private Border _dragSourceBorder;
        private object _draggedItem;
        private Point _dragStartPoint;
        private bool _isDragging;
        private double _dragStartLeft;
        private double _dragStartTop;
        private int _dragStartIndex;
        private int _currentTargetIndex = -1;
        private double _itemHeight = 80;
        private List<Border> _teamBorders = new List<Border>();

        public CompetitionResultsPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;

            // Подписываемся на событие смены темы
            App.ThemeChanged += OnThemeChanged;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CompetitionResultsViewModel viewModel)
            {
                viewModel.TeamsReordered += OnTeamsReordered;
                viewModel.PositionUpdated += OnPositionUpdated;
                CreateTeamCards();
            }
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CompetitionResultsViewModel viewModel)
            {
                viewModel.TeamsReordered -= OnTeamsReordered;
                viewModel.PositionUpdated -= OnPositionUpdated;
            }
        }

        // Обработчик смены темы
        private void OnThemeChanged(object sender, string themeName)
        {
            // Пересоздаем карточки с новыми цветами
            RefreshAllCards();
        }

        private void CreateTeamCards()
        {
            TeamsPanel.Children.Clear();
            _teamBorders.Clear();
            _selectedBorder = null;

            var viewModel = DataContext as CompetitionResultsViewModel;
            if (viewModel?.Teams == null) return;

            foreach (var team in viewModel.Teams)
            {
                var border = CreateTeamCard(team);
                TeamsPanel.Children.Add(border);
                _teamBorders.Add(border);
            }
        }

        private Border CreateTeamCard(TeamResultDto team)
        {
            var border = new Border
            {
                Background = GetCardBackground(team),
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(5),
                Height = _itemHeight,
                Cursor = Cursors.Hand,
                Tag = team
            };

            border.Effect = new DropShadowEffect
            {
                BlurRadius = 5,
                ShadowDepth = 2,
                Opacity = 0.15
            };

            // Создаем содержимое
            var grid = new Grid();
            grid.Margin = new Thickness(10);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Место
            var positionBorder = new Border
            {
                Background = GetPlaceBrush(team.Place ?? 0),
                CornerRadius = new CornerRadius(25),
                Width = 40,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var positionText = new TextBlock
            {
                Text = team.Place.ToString(),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            positionBorder.Child = positionText;
            Grid.SetColumn(positionBorder, 0);
            grid.Children.Add(positionBorder);

            // Название команды
            var nameText = new TextBlock
            {
                Text = team.TeamName,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Foreground = (Brush)FindResource("TextColor")
            };
            Grid.SetColumn(nameText, 1);
            grid.Children.Add(nameText);

            // Кнопка комментария
            var commentButton = new Button
            {
                Style = (Style)FindResource("SecondaryButton"),
                Content = "💬 Комментарий",
                Background = (Brush)FindResource("PrimaryBrush"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10, 5, 10, 5),
                Cursor = Cursors.Hand,
                Tag = team
            };
            commentButton.Click += CommentButton_Click;
            Grid.SetColumn(commentButton, 2);
            grid.Children.Add(commentButton);

            border.Child = grid;

            // Добавляем обработчики событий для Drag&Drop
            if (DataContext is CompetitionResultsViewModel vm && vm.IsEditMode)
            {
                border.PreviewMouseLeftButtonDown += CardBorder_PreviewMouseLeftButtonDown;
                border.MouseLeftButtonDown += CardBorder_MouseLeftButtonDown;
                border.MouseLeftButtonUp += CardBorder_MouseLeftButtonUp;
                border.MouseMove += CardBorder_MouseMove;
            }

            border.MouseEnter += CardBorder_MouseEnter;
            border.MouseLeave += CardBorder_MouseLeave;

            return border;
        }

        private Brush GetCardBackground(TeamResultDto team)
        {
            if (team.IsSaved)
            {
                return (Brush)FindResource("SuccessBrush");
            }
            return (Brush)FindResource("CardBackground");
        }

        private Brush GetPlaceBrush(int place)
        {
            return place switch
            {
                1 => new SolidColorBrush(Color.FromRgb(255, 215, 0)),   // Золотой
                2 => new SolidColorBrush(Color.FromRgb(192, 192, 192)), // Серебряный
                3 => new SolidColorBrush(Color.FromRgb(205, 127, 50)),  // Бронзовый
                _ => (Brush)FindResource("PrimaryBrush")
            };
        }

        private void UpdateCardDisplay(Border border, TeamResultDto team)
        {
            if (border?.Child is Grid grid)
            {
                // Обновляем место
                if (grid.Children[0] is Border positionBorder)
                {
                    positionBorder.Background = GetPlaceBrush(team.Place ?? 0);

                    if (positionBorder.Child is TextBlock positionText)
                    {
                        if (positionText.Text != team.PlaceDisplay)
                        {
                            AnimatePositionChange(positionText, team.PlaceDisplay);
                        }
                    }
                }

                // Обновляем фон всей карточки (используем динамический ресурс)
                border.Background = GetCardBackground(team);

                // Обновляем цвет кнопки комментария
                if (grid.Children[2] is Button commentButton)
                {
                    commentButton.Background = (Brush)FindResource("PrimaryBrush");
                    commentButton.Tag = team;
                }

                // Обновляем цвет текста названия команды
                if (grid.Children[1] is TextBlock nameText)
                {
                    nameText.Foreground = (Brush)FindResource("TextColor");
                }
            }
        }

        private void RefreshAllCards()
        {
            if (DataContext is CompetitionResultsViewModel viewModel)
            {
                for (int i = 0; i < _teamBorders.Count && i < viewModel.Teams.Count; i++)
                {
                    UpdateCardDisplay(_teamBorders[i], viewModel.Teams[i]);
                }
            }
        }

        private void AnimatePositionChange(TextBlock textBlock, string newText)
        {
            var scaleAnimation = new DoubleAnimation
            {
                From = 1,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(100),
                AutoReverse = true
            };

            var scaleTransform = new ScaleTransform();
            textBlock.RenderTransform = scaleTransform;
            textBlock.RenderTransformOrigin = new Point(0.5, 0.5);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            textBlock.Text = newText;
        }

        // Выделение плашки
        private void CardBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var team = border?.Tag as TeamResultDto;

            if (team != null && !_isDragging && DataContext is CompetitionResultsViewModel viewModel)
            {
                if (_selectedBorder != null)
                {
                    _selectedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    _selectedBorder.BorderThickness = new Thickness(1);
                }

                _selectedBorder = border;
                _selectedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                _selectedBorder.BorderThickness = new Thickness(2);

                viewModel.SelectTeamCommand.Execute(team);
            }
        }

        private void CardBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragSourceBorder = sender as Border;
            _draggedItem = _dragSourceBorder?.Tag;
            _dragStartPoint = e.GetPosition(this);

            if (_draggedItem is TeamResultDto team && DataContext is CompetitionResultsViewModel viewModel)
            {
                _dragStartIndex = viewModel.Teams.IndexOf(team);
                _currentTargetIndex = _dragStartIndex;

                Point startPos = _dragSourceBorder.TransformToAncestor(MainGrid).Transform(new Point(0, 0));
                _dragStartLeft = startPos.X;
                _dragStartTop = startPos.Y;
            }

            e.Handled = false;
        }

        private void CardBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedItem == null || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point currentPoint = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPoint;

            if (!_isDragging && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                StartDrag();
            }

            if (_isDragging)
            {
                UpdateDragPreviewPosition(currentPoint);
                Point posInPanel = Mouse.GetPosition(TeamsPanel);
                CheckDragPosition(posInPanel.Y);
            }
        }

        private void StartDrag()
        {
            _isDragging = true;

            if (_selectedBorder != null)
            {
                _selectedBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                _selectedBorder.BorderThickness = new Thickness(1);
                _selectedBorder = null;
            }

            SetupDragPreview();
            if (_dragSourceBorder != null)
                _dragSourceBorder.Opacity = 0.4;
            _dragSourceBorder?.CaptureMouse();
        }

        private void SetupDragPreview()
        {
            if (_draggedItem is not TeamResultDto team || _dragSourceBorder == null) return;

            DragPreviewName.Text = team.TeamName;
            DragPreviewPosition.Text = team.PlaceDisplay;

            DragPreview.Width = _dragSourceBorder.ActualWidth;
            DragPreview.Height = _itemHeight;

            DragCanvas.Visibility = Visibility.Visible;
            Canvas.SetLeft(DragPreview, _dragStartLeft);
            Canvas.SetTop(DragPreview, _dragStartTop);
        }

        private void UpdateDragPreviewPosition(Point mousePosition)
        {
            double newLeft = mousePosition.X - (_dragStartPoint.X - _dragStartLeft);
            double newTop = mousePosition.Y - (_dragStartPoint.Y - _dragStartTop);

            Canvas.SetLeft(DragPreview, newLeft);
            Canvas.SetTop(DragPreview, newTop);
        }

        private void CheckDragPosition(double y)
        {
            int targetIndex = GetTargetIndexFromPosition(y);

            if (targetIndex != -1 && targetIndex != _currentTargetIndex && DataContext is CompetitionResultsViewModel viewModel)
            {
                _currentTargetIndex = targetIndex;

                if (_dragStartIndex != _currentTargetIndex)
                {
                    int newPosition = _currentTargetIndex + 1;
                    DragPreviewPosition.Text = newPosition.ToString();
                    AnimateSlotRelease(_dragStartIndex, _currentTargetIndex);
                    viewModel.MoveTeam(_dragStartIndex, _currentTargetIndex);
                    _dragStartIndex = _currentTargetIndex;
                }
            }
        }

        private int GetTargetIndexFromPosition(double y)
        {
            for (int i = 0; i < _teamBorders.Count; i++)
            {
                var border = _teamBorders[i];
                Point position = border.TransformToAncestor(TeamsPanel).Transform(new Point(0, 0));
                double top = position.Y;
                double bottom = top + _itemHeight;

                if (y >= top && y <= bottom)
                {
                    return i;
                }
            }
            return -1;
        }

        private void AnimateSlotRelease(int fromIndex, int toIndex)
        {
            int direction = fromIndex < toIndex ? -1 : 1;
            int start = Math.Min(fromIndex, toIndex);
            int end = Math.Max(fromIndex, toIndex);

            for (int i = start; i <= end; i++)
            {
                if (i == toIndex) continue;

                var movingBorder = _teamBorders[i];
                if (movingBorder != null)
                {
                    if (movingBorder.RenderTransform is TranslateTransform oldTransform)
                    {
                        oldTransform.BeginAnimation(TranslateTransform.YProperty, null);
                    }

                    var transform = new TranslateTransform();
                    movingBorder.RenderTransform = transform;

                    double shift = direction * _itemHeight;
                    DoubleAnimation animation = new DoubleAnimation
                    {
                        From = shift,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    transform.BeginAnimation(TranslateTransform.YProperty, animation);
                }
            }
        }

        private void CardBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                EndDrag();
            }

            if (_dragSourceBorder != null && _isDragging)
            {
                _dragSourceBorder.ReleaseMouseCapture();
            }

            _isDragging = false;
            _currentTargetIndex = -1;
        }

        private void EndDrag()
        {
            DragCanvas.Visibility = Visibility.Collapsed;

            if (_dragSourceBorder != null)
            {
                _dragSourceBorder.Opacity = 1;
                _dragSourceBorder.ReleaseMouseCapture();
            }

            _dragSourceBorder = null;
            _draggedItem = null;
        }

        private void OnTeamsReordered(int fromIndex, int toIndex)
        {
            var border = _teamBorders[fromIndex];
            _teamBorders.RemoveAt(fromIndex);
            _teamBorders.Insert(toIndex, border);

            TeamsPanel.Children.Clear();
            foreach (var b in _teamBorders)
            {
                TeamsPanel.Children.Add(b);
            }

            // Обновляем отображение всех карточек
            if (DataContext is CompetitionResultsViewModel viewModel)
            {
                for (int i = 0; i < _teamBorders.Count && i < viewModel.Teams.Count; i++)
                {
                    UpdateCardDisplay(_teamBorders[i], viewModel.Teams[i]);
                }
            }
        }

        private void OnPositionUpdated(int index)
        {
            if (index < _teamBorders.Count && DataContext is CompetitionResultsViewModel viewModel)
            {
                UpdateCardDisplay(_teamBorders[index], viewModel.Teams[index]);
            }
        }

        private void CardBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null && border != _dragSourceBorder && !_isDragging && border != _selectedBorder)
            {
                border.Background = (Brush)FindResource("HoverColor");
            }
        }

        private void CardBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null && border != _dragSourceBorder && border != _selectedBorder)
            {
                var team = border.Tag as TeamResultDto;
                border.Background = GetCardBackground(team);
            }
        }

        private void CommentButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var team = button?.Tag as TeamResultDto;

            if (team != null && DataContext is CompetitionResultsViewModel viewModel)
            {
                viewModel.AddCommentCommand.Execute(team);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CompetitionResultsViewModel viewModel)
            {
                viewModel.TeamsReordered -= OnTeamsReordered;
                viewModel.PositionUpdated -= OnPositionUpdated;
                viewModel.Dispose();
            }

            // Отписываемся от события смены темы
            App.ThemeChanged -= OnThemeChanged;
        }
    }
}