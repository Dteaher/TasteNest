using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace RecipeKeeper.Wpf.Ui;

public static class MouseWheelScroll
{
    public static readonly DependencyProperty ForwardNestedWheelProperty =
        DependencyProperty.RegisterAttached(
            "ForwardNestedWheel",
            typeof(bool),
            typeof(MouseWheelScroll),
            new PropertyMetadata(false, OnForwardNestedWheelChanged));

    public static bool GetForwardNestedWheel(DependencyObject element) =>
        (bool)element.GetValue(ForwardNestedWheelProperty);

    public static void SetForwardNestedWheel(DependencyObject element, bool value) =>
        element.SetValue(ForwardNestedWheelProperty, value);

    private static void OnForwardNestedWheelChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not UIElement uiElement)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            uiElement.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            uiElement.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not DependencyObject source)
        {
            return;
        }

        var currentScrollViewer = source as ScrollViewer ?? FindDescendant<ScrollViewer>(source);
        if (currentScrollViewer is not null && CanScroll(currentScrollViewer, e.Delta))
        {
            return;
        }

        var parentScrollViewer = FindAncestor<ScrollViewer>(source is ScrollViewer ? VisualTreeHelper.GetParent(source) : source);
        if (parentScrollViewer is null)
        {
            return;
        }

        e.Handled = true;
        parentScrollViewer.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = UIElement.MouseWheelEvent,
            Source = sender
        });
    }

    private static bool CanScroll(ScrollViewer scrollViewer, int delta)
    {
        if (scrollViewer.ScrollableHeight <= 0)
        {
            return false;
        }

        return delta < 0
            ? scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight
            : scrollViewer.VerticalOffset > 0;
    }

    private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        var current = start;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindDescendant<T>(DependencyObject start) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++)
        {
            var child = VisualTreeHelper.GetChild(start, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindDescendant<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
