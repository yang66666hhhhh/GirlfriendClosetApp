using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Application.DTOs;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class WardrobeInsightsDialog : UserControl
{
    private readonly WardrobeInsightsDto _insights;

    public WardrobeInsightsDialog(WardrobeInsightsDto insights)
    {
        InitializeComponent();
        _insights = insights;

        Loaded += (_, _) => BindData();
    }

    private void BindData()
    {
        BindOverview();
        BindWearRate();
        BindTopWorn();
        BindDistribution();
        BindIdleOutfits();
    }

    private void BindOverview()
    {
        TotalWearCountText.Text = _insights.TotalWearCount.ToString();
        ActiveDaysText.Text = _insights.ActiveDays.ToString();
        StreakText.Text = _insights.CurrentStreak.ToString();
        StreakHintText.Text = _insights.StreakText;
    }

    private void BindWearRate()
    {
        WearRateValueText.Text = $"{_insights.WearRate}%";
        WornCountText.Text = $"已穿过: {_insights.WornOutfitCount} 套";
        NeverWornText.Text = $"从未穿过: {_insights.NeverWornCount} 套";

        var maxWidth = 460.0;
        WearRateBar.Width = maxWidth * _insights.WearRate / 100;
        WearRateBar.Background = _insights.WearRate >= 70
            ? new SolidColorBrush(Color.FromRgb(0x58, 0x81, 0xD6))
            : _insights.WearRate >= 40
                ? new SolidColorBrush(Color.FromRgb(0xCA, 0x9C, 0x9F))
                : new SolidColorBrush(Color.FromRgb(0xD6, 0x58, 0x58));
    }

    private void BindTopWorn()
    {
        if (_insights.TopWornOutfits.Count == 0)
        {
            TopWornList.Visibility = Visibility.Collapsed;
            NoTopWornText.Visibility = Visibility.Visible;
            return;
        }

        var items = _insights.TopWornOutfits
            .Select((item, index) => new TopWornDisplayItem(index + 1, item.Name, item.WearCountText, item.LastWornText))
            .ToList();
        TopWornList.ItemsSource = items;
    }

    private void BindDistribution()
    {
        BuildDistributionChips(SceneDistributionPanel, _insights.SceneDistribution);
        BuildDistributionChips(SeasonDistributionPanel, _insights.SeasonDistribution);
    }

    private void BuildDistributionChips(Panel panel, IReadOnlyList<DistributionItem> items)
    {
        foreach (var item in items)
        {
            var chip = new Border
            {
                Style = (Style)FindResource("DistributionChip"),
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

    private void BindIdleOutfits()
    {
        IdleSummaryText.Text = _insights.IdleSummaryText;

        if (_insights.IdleOutfits.Count == 0)
        {
            IdleList.Visibility = Visibility.Collapsed;
            NoIdleText.Visibility = Visibility.Visible;
            return;
        }

        IdleList.ItemsSource = _insights.IdleOutfits;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Services.ModalService.Instance.Hide();
    }
}

internal sealed record TopWornDisplayItem(int Rank, string Name, string WearCountText, string LastWornText);
