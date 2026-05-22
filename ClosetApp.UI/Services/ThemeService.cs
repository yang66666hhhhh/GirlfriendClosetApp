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

        UpdateColor("Shadow.Color", palette.ShadowColor);
        UpdateBrush("ShadowColorBrush", palette.ShadowColor);
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
                    Primary: Color.FromRgb(218, 148, 165),
                    PrimaryDark: Color.FromRgb(185, 108, 128),
                    PrimaryLight: Color.FromRgb(250, 232, 237),
                    PrimaryGlow: Color.FromArgb(60, 218, 148, 165),
                    SurfacePage: Color.FromRgb(252, 248, 245),
                    SurfaceModal: Color.FromRgb(255, 252, 250),
                    SurfaceCard: Color.FromRgb(255, 255, 255),
                    SurfaceHero: Color.FromRgb(250, 240, 236),
                    SurfaceSection: Color.FromRgb(253, 247, 243),
                    SurfaceElevated: Color.FromRgb(255, 255, 255),
                    SurfaceImageArea: Color.FromRgb(248, 241, 236),
                    BorderLight: Color.FromRgb(240, 228, 224),
                    BorderDivider: Color.FromRgb(246, 238, 234),
                    SidebarBackground: Color.FromRgb(254, 250, 247),
                    SidebarBorder: Color.FromRgb(244, 234, 230),
                    ShadowColor: Color.FromArgb(30, 160, 130, 110),
                    SkySurface: Color.FromRgb(250, 242, 240),
                    SkyBorder: Color.FromRgb(240, 218, 214),
                    SkyText: Color.FromRgb(165, 115, 125),
                    MintSurface: Color.FromRgb(252, 240, 235),
                    MintBorder: Color.FromRgb(242, 215, 206),
                    MintText: Color.FromRgb(170, 112, 96),
                    RoseSurface: Color.FromRgb(254, 235, 240),
                    RoseBorder: Color.FromRgb(245, 192, 205),
                    RoseText: Color.FromRgb(175, 88, 115),
                    AmberSurface: Color.FromRgb(254, 244, 230),
                    AmberBorder: Color.FromRgb(242, 215, 182),
                    AmberText: Color.FromRgb(165, 115, 55),
                    LavenderSurface: Color.FromRgb(250, 240, 248),
                    LavenderBorder: Color.FromRgb(232, 208, 227),
                    LavenderText: Color.FromRgb(145, 95, 138))
            };
        }
    }
}
