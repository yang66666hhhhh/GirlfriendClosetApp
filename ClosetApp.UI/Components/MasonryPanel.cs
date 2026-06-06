using System.Windows;
using System.Windows.Controls;

namespace ClosetApp.UI.Components;

public class MasonryPanel : Panel
{
    public static readonly DependencyProperty ColumnWidthProperty =
        DependencyProperty.Register(nameof(ColumnWidth), typeof(double), typeof(MasonryPanel),
            new FrameworkPropertyMetadata(280.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(MasonryPanel),
            new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double ColumnWidth
    {
        get => (double)GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double colW = ColumnWidth;
        double gap = Spacing;
        double usableWidth = double.IsInfinity(availableSize.Width)
            ? Children.Count * (colW + gap)
            : availableSize.Width;

        int columns = GetColumnCount(usableWidth, colW, gap);

        double[] columnHeights = new double[columns];

        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(colW, double.PositiveInfinity));

            int shortest = 0;
            for (int i = 1; i < columns; i++)
                if (columnHeights[i] < columnHeights[shortest])
                    shortest = i;

            columnHeights[shortest] += child.DesiredSize.Height + gap;
        }

        double totalHeight = columnHeights.Length > 0 ? columnHeights.Max() : 0;
        return new Size(usableWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double colW = ColumnWidth;
        double gap = Spacing;
        int columns = GetColumnCount(finalSize.Width, colW, gap);
        double totalContentWidth = columns * colW + (columns - 1) * gap;
        double offsetX = (finalSize.Width - totalContentWidth) / 2.0;

        double[] columnHeights = new double[columns];

        foreach (UIElement child in InternalChildren)
        {
            int shortest = 0;
            for (int i = 1; i < columns; i++)
                if (columnHeights[i] < columnHeights[shortest])
                    shortest = i;

            double x = offsetX + shortest * (colW + gap);
            double y = columnHeights[shortest];

            child.Arrange(new Rect(x, y, colW, child.DesiredSize.Height));

            columnHeights[shortest] += child.DesiredSize.Height + gap;
        }

        double maxHeight = columnHeights.Length > 0 ? columnHeights.Max() : 0;
        return new Size(finalSize.Width, maxHeight);
    }

    private int GetColumnCount(double width, double columnWidth, double gap)
    {
        int availableColumns = Math.Max(1, (int)((width + gap) / (columnWidth + gap)));
        return Math.Max(1, Math.Min(availableColumns, InternalChildren.Count));
    }
}
