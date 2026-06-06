using System.Windows;
using System.Windows.Media;
using ClosetApp.UI.Components;
using Xunit;

namespace ClosetApp.Tests;

public class MasonryPanelTests
{
    [Fact]
    public void Arrange_WithFewerChildrenThanAvailableColumns_CentersActualColumns()
    {
        RunOnStaThread(() =>
        {
            var panel = new MasonryPanel
            {
                ColumnWidth = 100,
                Spacing = 10
            };

            panel.Children.Add(new FixedSizeElement(100, 50));
            panel.Children.Add(new FixedSizeElement(100, 50));
            panel.Children.Add(new FixedSizeElement(100, 50));

            panel.Measure(new Size(430, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, 430, 80));
            panel.UpdateLayout();

            Assert.Equal(55, VisualTreeHelper.GetOffset(panel.Children[0]).X, precision: 1);
            Assert.Equal(165, VisualTreeHelper.GetOffset(panel.Children[1]).X, precision: 1);
            Assert.Equal(275, VisualTreeHelper.GetOffset(panel.Children[2]).X, precision: 1);
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
            throw exception;
    }

    private sealed class FixedSizeElement(double width, double height) : FrameworkElement
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(width, height);
        }
    }
}
