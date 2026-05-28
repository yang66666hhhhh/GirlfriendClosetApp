using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ClosetApp.Application.DTOs;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class AnnualOutfitReportDialog : UserControl
{
    private readonly AnnualOutfitReportDto _report;

    public AnnualOutfitReportDialog(AnnualOutfitReportDto report)
    {
        InitializeComponent();
        _report = report;

        Loaded += (_, _) => BindData();
    }

    private void BindData()
    {
        TitleText.Text = $"{_report.Year} 年度穿搭报告";
        TotalWearCountText.Text = _report.TotalWearCount.ToString();
        ActiveDaysText.Text = _report.ActiveDays.ToString();

        BindHighlights();
        BindTopOutfits();
        BindMonthlyStats();
        BindDistribution();
    }

    private void BindHighlights()
    {
        HighlightsList.ItemsSource = _report.Highlights;
    }

    private void BindTopOutfits()
    {
        var items = _report.Top5Outfits
            .Select((item, index) => new TopOutfitDisplayItem(
                index + 1,
                item.Name,
                item.SeasonText,
                item.SceneText,
                $"穿过 {item.WearCount} 次"))
            .ToList();
        TopOutfitsList.ItemsSource = items;
    }

    private void BindMonthlyStats()
    {
        MonthlyStatsList.ItemsSource = _report.MonthlyStats;

        // 设置月度条形图宽度
        MonthlyStatsList.ItemContainerGenerator.StatusChanged += (_, _) =>
        {
            if (MonthlyStatsList.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            var maxWear = _report.MonthlyStats.Count > 0
                ? _report.MonthlyStats.Max(m => m.WearCount)
                : 1;
            if (maxWear == 0) maxWear = 1;

            for (var i = 0; i < _report.MonthlyStats.Count; i++)
            {
                var container = MonthlyStatsList.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;

                var grid = FindVisualChild<Grid>(container);
                if (grid == null) continue;

                var monthBar = FindChildByName<Border>(grid, "MonthBar");
                if (monthBar != null)
                {
                    var ratio = (double)_report.MonthlyStats[i].WearCount / maxWear;
                    monthBar.Width = Math.Max(4, ratio * 200);
                }
            }
        };
    }

    private void BindDistribution()
    {
        BuildDistributionChips(SceneDistributionPanel, _report.SceneDistribution);
        BuildDistributionChips(SeasonDistributionPanel, _report.SeasonDistribution);
    }

    private void BuildDistributionChips(Panel panel, IReadOnlyList<DistributionItem> items)
    {
        foreach (var item in items)
        {
            var chip = new Border
            {
                Background = (Brush)FindResource("SurfaceHeroBrush"),
                BorderBrush = (Brush)FindResource("BorderLightBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 6),
                Child = new TextBlock
                {
                    Text = $"{item.Label} {item.Count}",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("TextPrimaryBrush")
                }
            };
            panel.Children.Add(chip);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Services.ModalService.Instance.Hide();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private static T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name) return fe;
            var found = FindChildByName<T>(child, name);
            if (found != null) return found;
        }
        return null;
    }
}

internal sealed record TopOutfitDisplayItem(int Rank, string Name, string SeasonText, string SceneText, string WearCountText);
