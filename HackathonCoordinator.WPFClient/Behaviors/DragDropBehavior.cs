using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HackathonCoordinator.WPFClient.Behaviors
{
    public static class DragDropBehavior
    {
        public static readonly DependencyProperty EnableDragDropProperty =
            DependencyProperty.RegisterAttached("EnableDragDrop", typeof(bool), typeof(DragDropBehavior),
                new PropertyMetadata(false, OnEnableDragDropChanged));

        public static bool GetEnableDragDrop(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableDragDropProperty);
        }

        public static void SetEnableDragDrop(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableDragDropProperty, value);
        }

        private static void OnEnableDragDropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemsControl itemsControl)
            {
                if ((bool)e.NewValue)
                {
                    itemsControl.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                    itemsControl.PreviewMouseMove += OnPreviewMouseMove;
                    itemsControl.Drop += OnDrop;
                    itemsControl.DragOver += OnDragOver;
                }
                else
                {
                    itemsControl.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                    itemsControl.PreviewMouseMove -= OnPreviewMouseMove;
                    itemsControl.Drop -= OnDrop;
                    itemsControl.DragOver -= OnDragOver;
                }
            }
        }

        private static Point _startPoint;
        private static object _draggedItem;

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var itemsControl = sender as ItemsControl;
            if (itemsControl == null) return;

            var point = e.GetPosition(null);
            if (Math.Abs(point.X - _startPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(point.Y - _startPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource == null) return;

            var container = FindVisualParent<Border>(originalSource, "TeamCard");
            if (container == null) return;

            _draggedItem = itemsControl.ItemContainerGenerator.ItemFromContainer(container);
            if (_draggedItem == null) return;

            DragDrop.DoDragDrop(itemsControl, _draggedItem, DragDropEffects.Move);
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            var itemsControl = sender as ItemsControl;
            if (itemsControl == null) return;

            var droppedData = e.Data.GetData(_draggedItem.GetType());
            if (droppedData == null) return;

            var targetContainer = FindVisualParent<Border>(e.OriginalSource as DependencyObject, "TeamCard");
            if (targetContainer == null) return;

            var targetItem = itemsControl.ItemContainerGenerator.ItemFromContainer(targetContainer);
            if (targetItem == null) return;

            var viewModel = itemsControl.DataContext as dynamic;
            if (viewModel != null)
            {
                viewModel.MoveTeam(_draggedItem, targetItem);
            }

            _draggedItem = null;
        }

        private static T FindVisualParent<T>(DependencyObject child, string name = null) where T : FrameworkElement
        {
            while (child != null)
            {
                if (child is T element && (string.IsNullOrEmpty(name) || element.Name == name))
                    return element;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
    }
}