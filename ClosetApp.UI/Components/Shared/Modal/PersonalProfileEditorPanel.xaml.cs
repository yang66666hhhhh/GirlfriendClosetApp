using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.Services;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class PersonalProfileEditorPanel : UserControl, IEditorPanel<PersonalProfileDto>
{
    private readonly IPersonalProfileService _service;
    private PersonalProfileDto? _currentProfile;
    private string? _avatarSourcePath;
    private string? _fullBodySourcePath;
    private bool _removeAvatarPhoto;
    private bool _removeFullBodyPhoto;

    public PersonalProfileEditorPanel()
    {
        _service = App.Services.GetRequiredService<IPersonalProfileService>();
        InitializeComponent();
        Loaded += PersonalProfileEditorPanel_Loaded;
    }

    public event EventHandler<EditorResult<PersonalProfileDto>>? EditorCompleted;

    private async void PersonalProfileEditorPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _currentProfile = await _service.GetCurrentAsync();
        BindProfile(_currentProfile);
    }

    private void BindProfile(PersonalProfileDto? profile)
    {
        TxtDisplayName.Text = profile?.DisplayName ?? string.Empty;
        TxtHeightCm.Text = profile?.HeightCm?.ToString() ?? string.Empty;
        TxtBodyShape.Text = profile?.BodyShape ?? string.Empty;
        TxtSkinTone.Text = profile?.SkinTone ?? string.Empty;
        TxtHairLength.Text = profile?.HairLength ?? string.Empty;
        TxtHairColor.Text = profile?.HairColor ?? string.Empty;
        TxtFaceFeaturesSummary.Text = profile?.FaceFeaturesSummary ?? string.Empty;
        TxtStyleKeywords.Text = profile?.StyleKeywords ?? string.Empty;
        TxtAvoidKeywords.Text = profile?.AvoidKeywords ?? string.Empty;
        ChkConsent.IsChecked = profile?.CloudUploadConsentAcceptedAt.HasValue == true;

        ApplyPreview(AvatarPreview, ResolveProfileImagePath(profile?.AvatarPhotoPath));
        ApplyPreview(FullBodyPreview, ResolveProfileImagePath(profile?.FullBodyPhotoPath));
        RefreshPromptPreview();
    }

    private static void ApplyPreview(Image image, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            image.Source = null;
            return;
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(path);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 720;
        bitmap.EndInit();
        bitmap.Freeze();
        image.Source = bitmap;
    }

    private static string? ResolveProfileImagePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        return Path.Combine(ClosetApp.Infrastructure.AppPaths.AiProfileDir, relativePath);
    }

    private void SelectAvatar_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectImageFile("选择头像照");
        if (path == null)
            return;

        _avatarSourcePath = path;
        _removeAvatarPhoto = false;
        ApplyPreview(AvatarPreview, path);
        RefreshPromptPreview();
    }

    private void SelectFullBody_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectImageFile("选择全身照");
        if (path == null)
            return;

        _fullBodySourcePath = path;
        _removeFullBodyPhoto = false;
        ApplyPreview(FullBodyPreview, path);
        RefreshPromptPreview();
    }

    private void RemoveAvatar_Click(object sender, RoutedEventArgs e)
    {
        _avatarSourcePath = null;
        _removeAvatarPhoto = true;
        AvatarPreview.Source = null;
        RefreshPromptPreview();
    }

    private void RemoveFullBody_Click(object sender, RoutedEventArgs e)
    {
        _fullBodySourcePath = null;
        _removeFullBodyPhoto = true;
        FullBodyPreview.Source = null;
        RefreshPromptPreview();
    }

    private void RefreshPromptPreview()
    {
        var profile = BuildPreviewProfile();
        TxtPromptPreview.Text = AiGenerationPromptBuilder.BuildProfilePreviewPrompt(profile);
    }

    private PersonalProfileDto BuildPreviewProfile()
    {
        int? heightCm = null;
        if (int.TryParse(TxtHeightCm.Text.Trim(), out var parsedHeight))
            heightCm = parsedHeight;

        return new PersonalProfileDto(
            _currentProfile?.Id ?? Guid.Empty,
            TxtDisplayName.Text,
            heightCm,
            TxtBodyShape.Text,
            TxtSkinTone.Text,
            TxtHairLength.Text,
            TxtHairColor.Text,
            TxtFaceFeaturesSummary.Text,
            TxtStyleKeywords.Text,
            TxtAvoidKeywords.Text,
            ResolvePreviewPhotoPath(_removeAvatarPhoto, _avatarSourcePath, _currentProfile?.AvatarPhotoPath),
            ResolvePreviewPhotoPath(_removeFullBodyPhoto, _fullBodySourcePath, _currentProfile?.FullBodyPhotoPath),
            ChkConsent.IsChecked == true
                ? _currentProfile?.CloudUploadConsentAcceptedAt ?? DateTime.Now
                : null);
    }

    private static string? ResolvePreviewPhotoPath(bool markedForRemoval, string? selectedSourcePath, string? persistedRelativePath)
    {
        if (markedForRemoval)
            return null;

        if (!string.IsNullOrWhiteSpace(selectedSourcePath))
            return selectedSourcePath;

        return persistedRelativePath;
    }

    private void PromptInputChanged(object sender, TextChangedEventArgs e)
    {
        RefreshPromptPreview();
    }

    private void PromptConsentChanged(object sender, RoutedEventArgs e)
    {
        RefreshPromptPreview();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int? heightCm = null;
            if (!string.IsNullOrWhiteSpace(TxtHeightCm.Text))
            {
                if (!int.TryParse(TxtHeightCm.Text.Trim(), out var parsedHeight))
                {
                    ToastService.Instance.ShowError("身高格式不对", "请输入有效的厘米数。");
                    TxtHeightCm.Focus();
                    return;
                }

                heightCm = parsedHeight;
            }

            var saved = await _service.SaveAsync(new SavePersonalProfileRequest(
                TxtDisplayName.Text,
                heightCm,
                TxtBodyShape.Text,
                TxtSkinTone.Text,
                TxtHairLength.Text,
                TxtHairColor.Text,
                TxtFaceFeaturesSummary.Text,
                TxtStyleKeywords.Text,
                TxtAvoidKeywords.Text,
                _avatarSourcePath,
                _fullBodySourcePath,
                ChkConsent.IsChecked == true,
                _removeAvatarPhoto,
                _removeFullBodyPhoto));

            ToastService.Instance.ShowSuccess("个人档案已保存");
            _currentProfile = saved;
            BindProfile(saved);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("个人档案保存失败", ex.Message);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        EditorCompleted?.Invoke(this, new EditorResult<PersonalProfileDto>(EditorResultType.Cancelled));
        Services.ModalService.Instance.Hide();
    }

    private static string? SelectImageFile(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.gif;*.bmp"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
