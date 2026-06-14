using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ClosetApp.Application.Interfaces;

namespace ClosetApp.UI.Services;

public sealed class ThemeService
{
    private readonly ThemePreferencesService _preferencesService;

    public ThemeService(ThemePreferencesService preferencesService, ICurrentUserContext? currentUserContext = null)
    {
        _preferencesService = preferencesService;
        if (currentUserContext != null)
            currentUserContext.CurrentUserChanged += CurrentUserContext_CurrentUserChanged;
    }

    public AppThemeKind CurrentTheme { get; private set; } = AppThemeKind.Rose;
    public AppFontSizeLevel CurrentFontSizeLevel { get; private set; } = AppFontSizeLevel.Standard;

    public event EventHandler<AppThemeKind>? ThemeChanged;
    public event EventHandler<AppFontSizeLevel>? FontSizeLevelChanged;

    public async Task InitializeAsync()
    {
        await ReloadPreferencesAsync(raiseThemeChanged: false, raiseFontSizeChanged: false).ConfigureAwait(false);
    }

    public async Task ApplyThemeAsync(AppThemeKind theme)
    {
        ApplyThemeCore(theme, CurrentFontSizeLevel, raiseThemeChanged: true, raiseFontSizeChanged: false);
        await _preferencesService.SaveAsync(new ThemePreferences
        {
            Theme = theme,
            FontSizeLevel = CurrentFontSizeLevel
        }).ConfigureAwait(false);
    }

    public async Task ApplyFontSizeAsync(AppFontSizeLevel level)
    {
        ApplyFontSizeCore(level, raiseChanged: true);
        await _preferencesService.SaveAsync(new ThemePreferences
        {
            Theme = CurrentTheme,
            FontSizeLevel = level
        }).ConfigureAwait(false);
    }

    private void CurrentUserContext_CurrentUserChanged(object? sender, CurrentUserChangedEventArgs e)
    {
        ReloadPreferencesAsync(raiseThemeChanged: true, raiseFontSizeChanged: true).GetAwaiter().GetResult();
    }

    private async Task ReloadPreferencesAsync(bool raiseThemeChanged, bool raiseFontSizeChanged)
    {
        var preferences = await _preferencesService.GetAsync().ConfigureAwait(false);
        ApplyThemeCore(preferences.Theme, preferences.FontSizeLevel, raiseThemeChanged, raiseFontSizeChanged);
    }

    private void ApplyThemeCore(
        AppThemeKind theme,
        AppFontSizeLevel fontSizeLevel,
        bool raiseThemeChanged,
        bool raiseFontSizeChanged)
    {
        var palette = ThemePalette.Create(theme);
        var typography = TypographyScale.Create(fontSizeLevel);
        var dispatcher = global::System.Windows.Application.Current?.Dispatcher;

        if (dispatcher == null || dispatcher.CheckAccess())
        {
            ApplyPalette(palette);
            ApplyTypography(typography);
        }
        else
        {
            _ = dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyPalette(palette);
                ApplyTypography(typography);
            }));
        }

        CurrentTheme = theme;
        CurrentFontSizeLevel = fontSizeLevel;
        if (raiseThemeChanged)
            ThemeChanged?.Invoke(this, theme);
        if (raiseFontSizeChanged)
            FontSizeLevelChanged?.Invoke(this, fontSizeLevel);
    }

    private void ApplyFontSizeCore(AppFontSizeLevel level, bool raiseChanged)
    {
        ApplyTypography(TypographyScale.Create(level));
        CurrentFontSizeLevel = level;
        if (raiseChanged)
            FontSizeLevelChanged?.Invoke(this, level);
    }

    private static void ApplyPalette(ThemePalette palette)
    {
        UpdateColor("Primary", palette.Primary);
        UpdateBrush("PrimaryBrush", palette.Primary);
        UpdateColor("Primary.Dark", palette.PrimaryDark);
        UpdateBrush("PrimaryDarkBrush", palette.PrimaryDark);
        UpdateColor("Primary.Light", palette.PrimaryLight);
        UpdateBrush("PrimaryLightBrush", palette.PrimaryLight);
        UpdateColor("Primary.Glow", palette.PrimaryGlow);
        UpdateBrush("PrimaryGlowBrush", palette.PrimaryGlow);

        UpdateColor("Surface.Page", palette.SurfacePage);
        UpdateBrush("SurfacePageBrush", palette.SurfacePage);
        UpdateColor("Surface.Modal", palette.SurfaceModal);
        UpdateBrush("SurfaceModalBrush", palette.SurfaceModal);
        UpdateColor("Surface.Card", palette.SurfaceCard);
        UpdateBrush("SurfaceCardBrush", palette.SurfaceCard);
        UpdateColor("Surface.Hero", palette.SurfaceHero);
        UpdateBrush("SurfaceHeroBrush", palette.SurfaceHero);
        UpdateColor("Surface.Section", palette.SurfaceSection);
        UpdateBrush("SurfaceSectionBrush", palette.SurfaceSection);
        UpdateColor("Surface.Elevated", palette.SurfaceElevated);
        UpdateBrush("SurfaceElevatedBrush", palette.SurfaceElevated);

        UpdateColor("Surface.ImageArea", palette.SurfaceImageArea);
        UpdateBrush("SurfaceImageAreaBrush", palette.SurfaceImageArea);

        UpdateColor("Border.Light", palette.BorderLight);
        UpdateBrush("BorderLightBrush", palette.BorderLight);
        UpdateColor("Border.Divider", palette.BorderDivider);
        UpdateBrush("BorderDividerBrush", palette.BorderDivider);

        UpdateBrush("SecondaryBrush", palette.PrimaryDark);
        UpdateBrush("BackgroundBrush", palette.SurfacePage);
        UpdateBrush("CardBackgroundBrush", palette.SurfaceCard);
        UpdateBrush("SecondaryBackgroundBrush", palette.SurfaceSection);
        UpdateBrush("BorderBrush", palette.BorderLight);
        UpdateBrush("BorderFocusBrush", palette.Primary);

        UpdateBrush("SidebarBgBrush", palette.SidebarBackground);
        UpdateBrush("SidebarBorderBrush", palette.SidebarBorder);

        UpdateColor("Theme.Sky.Surface", palette.SkySurface);
        UpdateBrush("ThemeSkySurfaceBrush", palette.SkySurface);
        UpdateColor("Theme.Sky.Border", palette.SkyBorder);
        UpdateBrush("ThemeSkyBorderBrush", palette.SkyBorder);
        UpdateColor("Theme.Sky.Text", palette.SkyText);
        UpdateBrush("ThemeSkyTextBrush", palette.SkyText);

        UpdateColor("Theme.Mint.Surface", palette.MintSurface);
        UpdateBrush("ThemeMintSurfaceBrush", palette.MintSurface);
        UpdateColor("Theme.Mint.Border", palette.MintBorder);
        UpdateBrush("ThemeMintBorderBrush", palette.MintBorder);
        UpdateColor("Theme.Mint.Text", palette.MintText);
        UpdateBrush("ThemeMintTextBrush", palette.MintText);

        UpdateColor("Theme.Rose.Surface", palette.RoseSurface);
        UpdateBrush("ThemeRoseSurfaceBrush", palette.RoseSurface);
        UpdateColor("Theme.Rose.Border", palette.RoseBorder);
        UpdateBrush("ThemeRoseBorderBrush", palette.RoseBorder);
        UpdateColor("Theme.Rose.Text", palette.RoseText);
        UpdateBrush("ThemeRoseTextBrush", palette.RoseText);

        UpdateColor("Theme.Amber.Surface", palette.AmberSurface);
        UpdateBrush("ThemeAmberSurfaceBrush", palette.AmberSurface);
        UpdateBrush("TagAmberSurfaceBrush", palette.AmberSurface);
        UpdateColor("Theme.Amber.Border", palette.AmberBorder);
        UpdateBrush("ThemeAmberBorderBrush", palette.AmberBorder);
        UpdateBrush("TagAmberBorderBrush", palette.AmberBorder);
        UpdateColor("Theme.Amber.Text", palette.AmberText);
        UpdateBrush("ThemeAmberTextBrush", palette.AmberText);
        UpdateBrush("TagAmberTextBrush", palette.AmberText);

        UpdateColor("Theme.Lavender.Surface", palette.LavenderSurface);
        UpdateBrush("ThemeLavenderSurfaceBrush", palette.LavenderSurface);
        UpdateColor("Theme.Lavender.Border", palette.LavenderBorder);
        UpdateBrush("ThemeLavenderBorderBrush", palette.LavenderBorder);
        UpdateColor("Theme.Lavender.Text", palette.LavenderText);
        UpdateBrush("ThemeLavenderTextBrush", palette.LavenderText);

        UpdateShadow("Shadow.Button", palette.Primary);
        UpdateShadow("Shadow.Hero", palette.Primary);

        UpdateColor("Shadow.Color", palette.ShadowColor);
        UpdateBrush("ShadowColorBrush", palette.ShadowColor);
    }

    private static void ApplyTypography(TypographyScale typography)
    {
        UpdateDouble("FontSize.Hero", typography.Hero);
        UpdateDouble("FontSize.PageTitle", typography.PageTitle);
        UpdateDouble("FontSize.SectionTitle", typography.SectionTitle);
        UpdateDouble("FontSize.Section", typography.Section);
        UpdateDouble("FontSize.Label", typography.Label);
        UpdateDouble("FontSize.Body", typography.Body);
        UpdateDouble("FontSize.Input", typography.Input);
        UpdateDouble("FontSize.Hint", typography.Hint);
        UpdateDouble("FontSize.Meta", typography.Meta);
        UpdateDouble("FontSize.Tiny", typography.Tiny);
        UpdateDouble("Button.FontSize.Small", typography.ButtonSmall);
        UpdateDouble("Button.FontSize.Medium", typography.ButtonMedium);
        UpdateDouble("Button.FontSize.Large", typography.ButtonLarge);
    }

    private static void UpdateDouble(string key, double value)
    {
        var resources = CurrentResources;
        if (resources == null)
            return;

        resources[key] = value;
    }

    private static void UpdateColor(string key, Color color)
    {
        var resources = CurrentResources;
        if (resources == null)
            return;

        if (resources[key] is Color)
            resources[key] = color;
    }

    private static void UpdateBrush(string key, Color color)
    {
        var resources = CurrentResources;
        if (resources == null)
            return;

        if (resources[key] is SolidColorBrush brush)
        {
            if (!brush.IsFrozen)
            {
                brush.Color = color;
                return;
            }

            var mutableBrush = brush.CloneCurrentValue();
            mutableBrush.Color = color;
            resources[key] = mutableBrush;
            return;
        }

        resources[key] = new SolidColorBrush(color);
    }

    private static void UpdateShadow(string key, Color color)
    {
        var resources = CurrentResources;
        if (resources == null)
            return;

        if (resources[key] is DropShadowEffect shadow)
        {
            if (!shadow.IsFrozen)
            {
                shadow.Color = color;
                return;
            }

            var mutableShadow = shadow.CloneCurrentValue();
            mutableShadow.Color = color;
            resources[key] = mutableShadow;
        }
    }

    // 单元测试可能没有创建 WPF Application，主题状态仍应可初始化与持久化。
    private static ResourceDictionary? CurrentResources => global::System.Windows.Application.Current?.Resources;

    private readonly record struct ThemePalette(
        Color Primary,
        Color PrimaryDark,
        Color PrimaryLight,
        Color PrimaryGlow,
        Color SurfacePage,
        Color SurfaceModal,
        Color SurfaceCard,
        Color SurfaceHero,
        Color SurfaceSection,
        Color SurfaceElevated,
        Color SurfaceImageArea,
        Color BorderLight,
        Color BorderDivider,
        Color SidebarBackground,
        Color SidebarBorder,
        Color ShadowColor,
        Color SkySurface,
        Color SkyBorder,
        Color SkyText,
        Color MintSurface,
        Color MintBorder,
        Color MintText,
        Color RoseSurface,
        Color RoseBorder,
        Color RoseText,
        Color AmberSurface,
        Color AmberBorder,
        Color AmberText,
        Color LavenderSurface,
        Color LavenderBorder,
        Color LavenderText)
    {
        public static ThemePalette Create(AppThemeKind theme)
        {
            return theme switch
            {
                AppThemeKind.Blue => new ThemePalette(
                    Primary: Color.FromRgb(88, 129, 214),
                    PrimaryDark: Color.FromRgb(55, 90, 170),
                    PrimaryLight: Color.FromRgb(224, 236, 255),
                    PrimaryGlow: Color.FromArgb(60, 88, 129, 214),
                    SurfacePage: Color.FromRgb(240, 245, 252),
                    SurfaceModal: Color.FromRgb(248, 251, 255),
                    SurfaceCard: Color.FromRgb(255, 255, 255),
                    SurfaceHero: Color.FromRgb(230, 238, 250),
                    SurfaceSection: Color.FromRgb(234, 241, 252),
                    SurfaceElevated: Color.FromRgb(255, 255, 255),
                    SurfaceImageArea: Color.FromRgb(238, 244, 252),
                    BorderLight: Color.FromRgb(205, 218, 240),
                    BorderDivider: Color.FromRgb(220, 230, 245),
                    SidebarBackground: Color.FromRgb(244, 248, 255),
                    SidebarBorder: Color.FromRgb(215, 226, 244),
                    ShadowColor: Color.FromArgb(30, 60, 80, 120),
                    SkySurface: Color.FromRgb(225, 236, 255),
                    SkyBorder: Color.FromRgb(175, 200, 245),
                    SkyText: Color.FromRgb(55, 90, 170),
                    MintSurface: Color.FromRgb(228, 245, 250),
                    MintBorder: Color.FromRgb(175, 215, 232),
                    MintText: Color.FromRgb(50, 110, 140),
                    RoseSurface: Color.FromRgb(240, 242, 252),
                    RoseBorder: Color.FromRgb(200, 210, 240),
                    RoseText: Color.FromRgb(70, 90, 155),
                    AmberSurface: Color.FromRgb(238, 244, 252),
                    AmberBorder: Color.FromRgb(205, 218, 238),
                    AmberText: Color.FromRgb(90, 110, 145),
                    LavenderSurface: Color.FromRgb(232, 238, 255),
                    LavenderBorder: Color.FromRgb(190, 202, 242),
                    LavenderText: Color.FromRgb(65, 85, 165)),
                _ => new ThemePalette(
                    Primary: Color.FromRgb(202, 156, 159),
                    PrimaryDark: Color.FromRgb(176, 132, 136),
                    PrimaryLight: Color.FromRgb(247, 240, 238),
                    PrimaryGlow: Color.FromArgb(60, 202, 156, 159),
                    SurfacePage: Color.FromRgb(249, 245, 241),
                    SurfaceModal: Color.FromRgb(255, 252, 250),
                    SurfaceCard: Color.FromRgb(255, 255, 255),
                    SurfaceHero: Color.FromRgb(247, 241, 237),
                    SurfaceSection: Color.FromRgb(251, 246, 243),
                    SurfaceElevated: Color.FromRgb(255, 255, 255),
                    SurfaceImageArea: Color.FromRgb(247, 242, 238),
                    BorderLight: Color.FromRgb(236, 226, 223),
                    BorderDivider: Color.FromRgb(242, 234, 231),
                    SidebarBackground: Color.FromRgb(252, 249, 246),
                    SidebarBorder: Color.FromRgb(240, 231, 228),
                    ShadowColor: Color.FromArgb(30, 146, 124, 118),
                    SkySurface: Color.FromRgb(249, 243, 241),
                    SkyBorder: Color.FromRgb(235, 224, 221),
                    SkyText: Color.FromRgb(148, 118, 121),
                    MintSurface: Color.FromRgb(249, 243, 239),
                    MintBorder: Color.FromRgb(235, 222, 215),
                    MintText: Color.FromRgb(156, 123, 113),
                    RoseSurface: Color.FromRgb(250, 242, 243),
                    RoseBorder: Color.FromRgb(230, 213, 216),
                    RoseText: Color.FromRgb(163, 120, 125),
                    AmberSurface: Color.FromRgb(250, 244, 236),
                    AmberBorder: Color.FromRgb(234, 221, 204),
                    AmberText: Color.FromRgb(162, 128, 93),
                    LavenderSurface: Color.FromRgb(247, 241, 244),
                    LavenderBorder: Color.FromRgb(225, 215, 221),
                    LavenderText: Color.FromRgb(145, 118, 137))
            };
        }
    }

    private readonly record struct TypographyScale(
        double Hero,
        double PageTitle,
        double SectionTitle,
        double Section,
        double Label,
        double Body,
        double Input,
        double Hint,
        double Meta,
        double Tiny,
        double ButtonSmall,
        double ButtonMedium,
        double ButtonLarge)
    {
        public static TypographyScale Create(AppFontSizeLevel level)
        {
            var multiplier = level switch
            {
                AppFontSizeLevel.Small => 0.92,
                AppFontSizeLevel.Comfortable => 1.08,
                AppFontSizeLevel.Large => 1.16,
                AppFontSizeLevel.ExtraLarge => 1.24,
                _ => 1.0
            };

            static double Scale(double baseSize, double multiplier) => Math.Round(baseSize * multiplier, 1);

            return new TypographyScale(
                Hero: Scale(28, multiplier),
                PageTitle: Scale(20, multiplier),
                SectionTitle: Scale(18, multiplier),
                Section: Scale(11, multiplier),
                Label: Scale(13, multiplier),
                Body: Scale(14, multiplier),
                Input: Scale(15, multiplier),
                Hint: Scale(12, multiplier),
                Meta: Scale(11, multiplier),
                Tiny: Scale(10, multiplier),
                ButtonSmall: Scale(12, multiplier),
                ButtonMedium: Scale(14, multiplier),
                ButtonLarge: Scale(16, multiplier));
        }
    }
}
