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
                    Primary: Color.FromRgb(125, 159, 211),
                    PrimaryDark: Color.FromRgb(95, 130, 186),
                    PrimaryLight: Color.FromRgb(231, 239, 251),
                    PrimaryGlow: Color.FromArgb(51, 125, 159, 211),
                    SurfacePage: Color.FromRgb(242, 246, 252),
                    SurfaceModal: Color.FromRgb(252, 253, 255),
                    SurfaceCard: Color.FromRgb(252, 253, 255),
                    SurfaceHero: Color.FromRgb(237, 243, 250),
                    SurfaceSection: Color.FromRgb(245, 248, 252),
                    SurfaceElevated: Color.FromRgb(255, 255, 255),
                    BorderLight: Color.FromRgb(216, 226, 239),
                    BorderDivider: Color.FromRgb(229, 236, 244),
                    SidebarBackground: Color.FromRgb(247, 250, 254),
                    SidebarBorder: Color.FromRgb(224, 232, 242),
                    SkySurface: Color.FromRgb(237, 244, 255),
                    SkyBorder: Color.FromRgb(196, 215, 244),
                    SkyText: Color.FromRgb(86, 118, 170),
                    MintSurface: Color.FromRgb(236, 247, 246),
                    MintBorder: Color.FromRgb(196, 225, 221),
                    MintText: Color.FromRgb(82, 130, 127),
                    RoseSurface: Color.FromRgb(243, 239, 250),
                    RoseBorder: Color.FromRgb(216, 208, 238),
                    RoseText: Color.FromRgb(108, 98, 155),
                    AmberSurface: Color.FromRgb(245, 240, 231),
                    AmberBorder: Color.FromRgb(226, 214, 190),
                    AmberText: Color.FromRgb(138, 119, 86),
                    LavenderSurface: Color.FromRgb(236, 240, 251),
                    LavenderBorder: Color.FromRgb(201, 211, 238),
                    LavenderText: Color.FromRgb(95, 111, 166)),
                _ => new ThemePalette(
                    Primary: Color.FromRgb(217, 162, 153),
                    PrimaryDark: Color.FromRgb(196, 139, 129),
                    PrimaryLight: Color.FromRgb(242, 230, 227),
                    PrimaryGlow: Color.FromArgb(51, 217, 162, 153),
                    SurfacePage: Color.FromRgb(246, 243, 238),
                    SurfaceModal: Color.FromRgb(255, 252, 249),
                    SurfaceCard: Color.FromRgb(253, 252, 252),
                    SurfaceHero: Color.FromRgb(245, 240, 235),
                    SurfaceSection: Color.FromRgb(248, 245, 240),
                    SurfaceElevated: Color.FromRgb(255, 255, 255),
                    BorderLight: Color.FromRgb(232, 226, 220),
                    BorderDivider: Color.FromRgb(240, 236, 231),
                    SidebarBackground: Color.FromRgb(250, 248, 245),
                    SidebarBorder: Color.FromRgb(240, 235, 230),
                    SkySurface: Color.FromRgb(238, 244, 255),
                    SkyBorder: Color.FromRgb(203, 217, 244),
                    SkyText: Color.FromRgb(95, 121, 173),
                    MintSurface: Color.FromRgb(238, 247, 241),
                    MintBorder: Color.FromRgb(201, 224, 207),
                    MintText: Color.FromRgb(94, 138, 103),
                    RoseSurface: Color.FromRgb(252, 237, 239),
                    RoseBorder: Color.FromRgb(238, 198, 204),
                    RoseText: Color.FromRgb(176, 111, 125),
                    AmberSurface: Color.FromRgb(252, 243, 232),
                    AmberBorder: Color.FromRgb(235, 214, 186),
                    AmberText: Color.FromRgb(166, 121, 66),
                    LavenderSurface: Color.FromRgb(242, 239, 251),
                    LavenderBorder: Color.FromRgb(215, 207, 239),
                    LavenderText: Color.FromRgb(127, 106, 174))
            };
        }
    }
}
