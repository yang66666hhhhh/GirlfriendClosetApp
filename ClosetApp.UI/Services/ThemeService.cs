using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ClosetApp.UI.Services;

public sealed class ThemeService
{
    private readonly ThemePreferencesService _preferencesService;

    public ThemeService(ThemePreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    public AppThemeKind CurrentTheme { get; private set; } = AppThemeKind.Rose;

    public event EventHandler<AppThemeKind>? ThemeChanged;

    public async Task InitializeAsync()
    {
        var preferences = await _preferencesService.GetAsync().ConfigureAwait(false);
        ApplyThemeCore(preferences.Theme, raiseChanged: false);
    }

    public async Task ApplyThemeAsync(AppThemeKind theme)
    {
        ApplyThemeCore(theme, raiseChanged: true);
        await _preferencesService.SaveAsync(new ThemePreferences
        {
            Theme = theme
        }).ConfigureAwait(false);
    }

    private void ApplyThemeCore(AppThemeKind theme, bool raiseChanged)
    {
        var palette = ThemePalette.Create(theme);
        var dispatcher = global::System.Windows.Application.Current?.Dispatcher;

        if (dispatcher == null || dispatcher.CheckAccess())
        {
            ApplyPalette(palette);
        }
        else
        {
            _ = dispatcher.BeginInvoke(new Action(() => ApplyPalette(palette)));
        }

        CurrentTheme = theme;
        if (raiseChanged)
            ThemeChanged?.Invoke(this, theme);
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
        UpdateColor("Theme.Amber.Border", palette.AmberBorder);
        UpdateBrush("ThemeAmberBorderBrush", palette.AmberBorder);
        UpdateColor("Theme.Amber.Text", palette.AmberText);
        UpdateBrush("ThemeAmberTextBrush", palette.AmberText);

        UpdateColor("Theme.Lavender.Surface", palette.LavenderSurface);
        UpdateBrush("ThemeLavenderSurfaceBrush", palette.LavenderSurface);
        UpdateColor("Theme.Lavender.Border", palette.LavenderBorder);
        UpdateBrush("ThemeLavenderBorderBrush", palette.LavenderBorder);
        UpdateColor("Theme.Lavender.Text", palette.LavenderText);
        UpdateBrush("ThemeLavenderTextBrush", palette.LavenderText);

        UpdateShadow("Shadow.Button", palette.Primary);
        UpdateShadow("Shadow.Hero", palette.Primary);
    }

    private static void UpdateColor(string key, Color color)
    {
        if (global::System.Windows.Application.Current?.Resources[key] is Color)
            global::System.Windows.Application.Current.Resources[key] = color;
    }

    private static void UpdateBrush(string key, Color color)
    {
        if (global::System.Windows.Application.Current?.Resources[key] is SolidColorBrush brush)
        {
            if (!brush.IsFrozen)
            {
                brush.Color = color;
                return;
            }

            var mutableBrush = brush.CloneCurrentValue();
            mutableBrush.Color = color;
            global::System.Windows.Application.Current.Resources[key] = mutableBrush;
            return;
        }

        global::System.Windows.Application.Current!.Resources[key] = new SolidColorBrush(color);
    }

    private static void UpdateShadow(string key, Color color)
    {
        if (global::System.Windows.Application.Current?.Resources[key] is DropShadowEffect shadow)
        {
            if (!shadow.IsFrozen)
            {
                shadow.Color = color;
                return;
            }

            var mutableShadow = shadow.CloneCurrentValue();
            mutableShadow.Color = color;
            global::System.Windows.Application.Current.Resources[key] = mutableShadow;
        }
    }

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
        Color BorderLight,
        Color BorderDivider,
        Color SidebarBackground,
        Color SidebarBorder,
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
                    PrimaryDark: Color.FromRgb(62, 98, 177),
                    PrimaryLight: Color.FromRgb(224, 236, 255),
                    PrimaryGlow: Color.FromArgb(74, 88, 129, 214),
                    SurfacePage: Color.FromRgb(235, 241, 251),
                    SurfaceModal: Color.FromRgb(247, 250, 255),
                    SurfaceCard: Color.FromRgb(249, 252, 255),
                    SurfaceHero: Color.FromRgb(227, 236, 251),
                    SurfaceSection: Color.FromRgb(239, 245, 255),
                    SurfaceElevated: Color.FromRgb(255, 255, 255),
                    BorderLight: Color.FromRgb(200, 214, 237),
                    BorderDivider: Color.FromRgb(216, 228, 245),
                    SidebarBackground: Color.FromRgb(241, 246, 255),
                    SidebarBorder: Color.FromRgb(210, 223, 242),
                    SkySurface: Color.FromRgb(228, 238, 255),
                    SkyBorder: Color.FromRgb(179, 202, 243),
                    SkyText: Color.FromRgb(68, 108, 188),
                    MintSurface: Color.FromRgb(230, 243, 248),
                    MintBorder: Color.FromRgb(183, 217, 229),
                    MintText: Color.FromRgb(66, 121, 147),
                    RoseSurface: Color.FromRgb(234, 238, 252),
                    RoseBorder: Color.FromRgb(193, 203, 236),
                    RoseText: Color.FromRgb(84, 103, 166),
                    AmberSurface: Color.FromRgb(237, 242, 249),
                    AmberBorder: Color.FromRgb(201, 212, 230),
                    AmberText: Color.FromRgb(104, 119, 151),
                    LavenderSurface: Color.FromRgb(230, 235, 255),
                    LavenderBorder: Color.FromRgb(186, 197, 240),
                    LavenderText: Color.FromRgb(79, 97, 178)),
                _ => new ThemePalette(
                    Primary: Color.FromRgb(218, 148, 165),
                    PrimaryDark: Color.FromRgb(191, 114, 133),
                    PrimaryLight: Color.FromRgb(247, 227, 232),
                    PrimaryGlow: Color.FromArgb(74, 218, 148, 165),
                    SurfacePage: Color.FromRgb(248, 241, 237),
                    SurfaceModal: Color.FromRgb(255, 250, 247),
                    SurfaceCard: Color.FromRgb(255, 251, 249),
                    SurfaceHero: Color.FromRgb(247, 236, 231),
                    SurfaceSection: Color.FromRgb(250, 244, 240),
                    SurfaceElevated: Color.FromRgb(255, 255, 255),
                    BorderLight: Color.FromRgb(236, 223, 220),
                    BorderDivider: Color.FromRgb(244, 234, 230),
                    SidebarBackground: Color.FromRgb(252, 247, 243),
                    SidebarBorder: Color.FromRgb(242, 231, 228),
                    SkySurface: Color.FromRgb(248, 239, 237),
                    SkyBorder: Color.FromRgb(236, 214, 210),
                    SkyText: Color.FromRgb(171, 121, 130),
                    MintSurface: Color.FromRgb(250, 238, 233),
                    MintBorder: Color.FromRgb(239, 212, 203),
                    MintText: Color.FromRgb(176, 118, 102),
                    RoseSurface: Color.FromRgb(252, 232, 237),
                    RoseBorder: Color.FromRgb(241, 188, 201),
                    RoseText: Color.FromRgb(181, 95, 122),
                    AmberSurface: Color.FromRgb(252, 241, 226),
                    AmberBorder: Color.FromRgb(238, 211, 177),
                    AmberText: Color.FromRgb(171, 120, 61),
                    LavenderSurface: Color.FromRgb(247, 236, 244),
                    LavenderBorder: Color.FromRgb(229, 204, 223),
                    LavenderText: Color.FromRgb(151, 101, 143))
            };
        }
    }
}
