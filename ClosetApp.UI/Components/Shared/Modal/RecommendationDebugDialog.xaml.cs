using System.Windows;
using System.Windows.Controls;
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
        BreakdownList.ItemsSource = _debug.Breakdown;
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
