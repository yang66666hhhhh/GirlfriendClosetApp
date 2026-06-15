using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.ViewModels;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class AppearanceSettingsPanel : UserControl
{
    private readonly ThemeService _themeService;
    private bool _isApplyingSelection;

    public AppearanceSettingsPanel()
    {
        InitializeComponent();
        _themeService = App.Services.GetRequiredService<ThemeService>();
    }

    public void Refresh()
    {
        ApplyThemeCardSelection(_themeService.CurrentTheme);
        if (DataContext is SettingsViewModel viewModel)
        {
            ApplyOutfitCardDisplaySelection(viewModel.DefaultOutfitCardDisplayMode);
            ApplyFontPresetSelection(viewModel.FontSizePreset);
        }
    }

    public event EventHandler<AppThemeKind>? ThemeChanged;
    public event EventHandler<OutfitCardDisplayMode>? OutfitCardDisplayModeChanged;
    public event EventHandler<AppFontSizeLevel>? FontSizeLevelChanged;

    private void ThemeCard_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Components.Shared.ThemeCard card)
            return;
        ThemeChanged?.Invoke(this, card.ThemeKind);
    }

    public void ApplyThemeCardSelection(AppThemeKind theme)
    {
        ThemeRoseCard.IsSelected = theme == AppThemeKind.Rose;
        ThemeBlueCard.IsSelected = theme == AppThemeKind.Blue;
    }

    public void ApplyOutfitCardDisplaySelection(OutfitCardDisplayMode mode)
    {
        ApplySelectionSilently(() =>
        {
            RadioOutfitFirst.IsChecked = mode == OutfitCardDisplayMode.OutfitFirst;
            RadioEffectImageFirst.IsChecked = mode == OutfitCardDisplayMode.EffectImageFirst;
        });
    }

    public void ApplyFontPresetSelection(string preset)
    {
        ApplySelectionSilently(() =>
        {
            RadioFontCompact.IsChecked = string.Equals(preset, "Compact", StringComparison.Ordinal);
            RadioFontBalanced.IsChecked = string.Equals(preset, "Balanced", StringComparison.Ordinal);
            RadioFontExpanded.IsChecked = string.Equals(preset, "Expanded", StringComparison.Ordinal);
        });
    }

    private void ApplySelectionSilently(Action apply)
    {
        var wasApplying = _isApplyingSelection;
        _isApplyingSelection = true;
        try
        {
            apply();
        }
        finally
        {
            _isApplyingSelection = wasApplying;
        }
    }

    private void OutfitDisplayMode_Checked(object sender, RoutedEventArgs e)
    {
        if (_isApplyingSelection)
            return;

        if (sender is not RadioButton radioButton || radioButton.IsChecked != true)
            return;

        if (Equals(radioButton.Content, "效果图卡片"))
        {
            OutfitCardDisplayModeChanged?.Invoke(this, OutfitCardDisplayMode.EffectImageFirst);
            return;
        }

        OutfitCardDisplayModeChanged?.Invoke(this, OutfitCardDisplayMode.OutfitFirst);
    }

    private void FontSizeLevel_Checked(object sender, RoutedEventArgs e)
    {
        if (_isApplyingSelection)
            return;

        if (sender is not RadioButton { IsChecked: true, Tag: string levelName })
            return;

        var level = MapFontPresetToLevel(levelName);
        FontSizeLevelChanged?.Invoke(this, level);
    }

    private static AppFontSizeLevel MapFontPresetToLevel(string preset)
    {
        return preset switch
        {
            "Compact" => AppFontSizeLevel.Small,
            "Expanded" => AppFontSizeLevel.Large,
            _ => AppFontSizeLevel.Comfortable
        };
    }
}
