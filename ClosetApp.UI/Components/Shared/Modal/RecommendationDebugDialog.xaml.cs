using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class RecommendationDebugDialog : UserControl
{
    private readonly RecommendationDebugDto _debug;

    public RecommendationDebugDialog(RecommendationDebugDto debug)
    {
        InitializeComponent();
        _debug = debug;

        Loaded += (_, _) => BindData();
    }

    private void BindData()
    {
        SubtitleText.Text = _debug.OutfitName;
        TotalScoreText.Text = _debug.TotalScore.ToString();

        BindBreakdown();
        BindReasons();
        BindPreferenceWeights();
    }

    private void BindBreakdown()
    {
        var items = _debug.Breakdown;
        BreakdownList.ItemsSource = items;

        var maxAbs = items.Count > 0
            ? items.Max(i => Math.Abs(i.Score))
            : 1;
        if (maxAbs == 0) maxAbs = 1;

        BreakdownList.ItemContainerGenerator.StatusChanged += (_, _) =>
        {
            if (BreakdownList.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            for (var i = 0; i < items.Count; i++)
            {
                var container = BreakdownList.ItemContainerGenerator.ContainerFromIndex(i) as ContentPresenter;
                if (container == null) continue;

                var grid = FindVisualChild<Grid>(container);
                if (grid == null) continue;

                var barFill = FindChildByName<Border>(grid, "BarFill");
                var scoreSign = FindChildByName<TextBlock>(grid, "ScoreSignText");

                if (barFill != null)
                {
                    var ratio = (double)Math.Abs(items[i].Score) / maxAbs;
                    var maxWidth = 200.0;
                    barFill.Width = Math.Max(8, ratio * maxWidth);
                    barFill.Background = items[i].Score >= 0
                        ? new SolidColorBrush(ColorFromHex("#5881D6"))
                        : new SolidColorBrush(ColorFromHex("#D65858"));
                }

                if (scoreSign != null)
                {
                    scoreSign.Text = items[i].Score >= 0 ? "+" : "";
                    scoreSign.Foreground = items[i].Score >= 0
                        ? new SolidColorBrush(ColorFromHex("#5881D6"))
                        : new SolidColorBrush(ColorFromHex("#D65858"));
                }
            }
        };
    }

    private void BindReasons()
    {
        ReasonsList.ItemsSource = _debug.Reasons;
    }

    private void BindPreferenceWeights()
    {
        if (_debug.TotalPreferenceWeight <= 0)
        {
            var emptyText = new TextBlock
            {
                Text = "暂无偏好数据，多穿几次搭配后会自动学习。",
                FontSize = 12,
                Foreground = (Brush)FindResource("TextSecondaryBrush")
            };
            SceneWeightsPanel.Children.Add(emptyText);
            return;
        }

        BuildWeightSection(SceneWeightsPanel, "场景", _debug.SceneWeights
            .OrderByDescending(kv => kv.Value)
            .Select(kv => (kv.Key.GetDisplayName(), kv.Value))
            .ToList());

        BuildWeightSection(TagWeightsPanel, "标签", _debug.TagWeights
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select(kv => (kv.Key, kv.Value))
            .ToList());

        BuildWeightSection(ColorWeightsPanel, "颜色", _debug.ColorWeights
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select(kv => (kv.Key, kv.Value))
            .ToList());
    }

    private void BuildWeightSection(Panel panel, string title, IReadOnlyList<(string Name, int Weight)> items)
    {
        if (items.Count == 0) return;

        var header = new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 6)
        };
        panel.Children.Add(header);

        var wrapPanel = new WrapPanel();
        foreach (var (name, weight) in items)
        {
            var chip = new Border
            {
                Style = (Style)FindResource("DebugChip"),
                Background = (Brush)FindResource("SurfaceHeroBrush"),
                BorderBrush = (Brush)FindResource("BorderLightBrush"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 6, 6),
                Child = new TextBlock
                {
                    Text = $"{name} ({weight})",
                    Style = (Style)FindResource("DebugChipText"),
                    Foreground = (Brush)FindResource("TextPrimaryBrush")
                }
            };
            wrapPanel.Children.Add(chip);
        }
        panel.Children.Add(wrapPanel);
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

    private static Color ColorFromHex(string hex)
    {
        return (Color)ColorConverter.ConvertFromString(hex);
    }
}

internal static class OutfitSceneExtensions
{
    public static string GetDisplayName(this OutfitScene scene) => scene switch
    {
        OutfitScene.Work => "通勤",
        OutfitScene.Date => "约会",
        OutfitScene.Travel => "出游",
        OutfitScene.Party => "派对",
        OutfitScene.Casual => "休闲",
        _ => scene.ToString()
    };
}
