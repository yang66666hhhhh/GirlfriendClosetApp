using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.Services;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class PersonalCenterDialog : UserControl
{
    private readonly ILocalUserService _localUserService;
    private readonly ILocalAuthService _localAuthService;
    private readonly IPersonalProfileService _personalProfileService;
    private readonly ICurrentUserContext _currentUserContext;

    private Guid _currentUserId;
    private string? _accountAvatarSourcePath;
    private bool _removeAccountAvatarPhoto;
    private PersonalProfileDto? _currentProfile;
    private string? _avatarSourcePath;
    private string? _fullBodySourcePath;
    private bool _removeAvatarPhoto;
    private bool _removeFullBodyPhoto;
    private string? _effectiveAccountAvatarPath;

    public PersonalCenterDialog()
    {
        _localUserService = App.Services.GetRequiredService<ILocalUserService>();
        _localAuthService = App.Services.GetRequiredService<ILocalAuthService>();
        _personalProfileService = App.Services.GetRequiredService<IPersonalProfileService>();
        _currentUserContext = App.Services.GetRequiredService<ICurrentUserContext>();
        InitializeComponent();
        Loaded += PersonalCenterDialog_Loaded;
    }

    private async void PersonalCenterDialog_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var user = await _localUserService.GetCurrentAsync();
        _currentUserId = user.Id;
        _currentProfile = await _personalProfileService.GetCurrentAsync();
        _effectiveAccountAvatarPath = user.AvatarPhotoPath;

        BindUser(user);
        BindProfile(_currentProfile);
        ShowSection(AccountPanel);
    }

    private void BindUser(Domain.Entities.LocalUser user)
    {
        TxtHeaderName.Text = "个人中心";
        TxtHeaderMeta.Text = $"@{user.AccountName} · {(user.Role == LocalUserRole.SuperAdmin ? "超级管理员" : "本地用户")}";
        TxtSummaryAccountName.Text = $"@{user.AccountName}";
        TxtSummaryRole.Text = user.Role == LocalUserRole.SuperAdmin ? "超级管理员" : "本地用户";
        AccountAvatar.IsCurrent = true;
        AccountAvatarPreview.IsCurrent = false;

        TxtAccountName.Text = user.AccountName;
        TxtDisplayName.Text = user.DisplayName;
        RefreshAccountIdentityPreview();
    }

    private void BindProfile(PersonalProfileDto? profile)
    {
        TxtProfileDisplayName.Text = profile?.DisplayName ?? TxtDisplayName.Text;
        TxtHeightCm.Text = profile?.HeightCm?.ToString() ?? string.Empty;
        TxtBodyShape.Text = profile?.BodyShape ?? string.Empty;
        TxtSkinTone.Text = profile?.SkinTone ?? string.Empty;
        TxtHairLength.Text = profile?.HairLength ?? string.Empty;
        TxtHairColor.Text = profile?.HairColor ?? string.Empty;
        TxtFaceFeaturesSummary.Text = profile?.FaceFeaturesSummary ?? string.Empty;
        TxtStyleKeywords.Text = profile?.StyleKeywords ?? string.Empty;
        TxtAvoidKeywords.Text = profile?.AvoidKeywords ?? string.Empty;
        ChkConsent.IsChecked = profile?.CloudUploadConsentAcceptedAt.HasValue == true;

        ApplyPreview(AvatarPreview, ResolveProfilePreviewPath(profile?.AvatarPhotoPath));
        ApplyPreview(FullBodyPreview, ResolveProfilePreviewPath(profile?.FullBodyPhotoPath));
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

    private string? ResolveProfilePreviewPath(string? avatarPath)
    {
        if (_removeAvatarPhoto)
            return null;

        if (!string.IsNullOrWhiteSpace(_avatarSourcePath))
            return _avatarSourcePath;

        if (!string.IsNullOrWhiteSpace(avatarPath))
            return ResolveStoredImagePath(avatarPath);

        if (_removeAccountAvatarPhoto)
            return null;

        if (!string.IsNullOrWhiteSpace(_accountAvatarSourcePath))
            return _accountAvatarSourcePath;

        return ResolveStoredImagePath(_effectiveAccountAvatarPath);
    }

    private static string? ResolveStoredImagePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        return Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(ClosetApp.Infrastructure.AppPaths.AiProfileDir, relativePath);
    }

    private void AccountTab_Click(object sender, RoutedEventArgs e) => ShowSection(AccountPanel);

    private void ProfileTab_Click(object sender, RoutedEventArgs e) => ShowSection(ProfilePanel);

    private void SecurityTab_Click(object sender, RoutedEventArgs e) => ShowSection(SecurityPanel);

    private void ShowSection(UIElement section)
    {
        AccountPanel.Visibility = section == AccountPanel ? Visibility.Visible : Visibility.Collapsed;
        ProfilePanel.Visibility = section == ProfilePanel ? Visibility.Visible : Visibility.Collapsed;
        SecurityPanel.Visibility = section == SecurityPanel ? Visibility.Visible : Visibility.Collapsed;

        ApplyTabStyle(BtnAccountTab, section == AccountPanel);
        ApplyTabStyle(BtnProfileTab, section == ProfilePanel);
        ApplyTabStyle(BtnSecurityTab, section == SecurityPanel);
    }

    private void ApplyTabStyle(Button button, bool selected)
    {
        button.Background = selected
            ? (Brush)FindResource("PrimaryLightBrush")
            : (Brush)FindResource("SurfaceCardBrush");
        button.BorderBrush = selected
            ? (Brush)FindResource("PrimaryBrush")
            : (Brush)FindResource("BorderLightBrush");
        button.Foreground = selected
            ? (Brush)FindResource("PrimaryBrush")
            : (Brush)FindResource("TextPrimaryBrush");
    }

    private void SelectAccountAvatar_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectImageFile("选择账号头像");
        if (path == null)
            return;

        _accountAvatarSourcePath = path;
        _removeAccountAvatarPhoto = false;
        RefreshAccountIdentityPreview();
    }

    private void RemoveAccountAvatar_Click(object sender, RoutedEventArgs e)
    {
        _accountAvatarSourcePath = null;
        _removeAccountAvatarPhoto = true;
        RefreshAccountIdentityPreview();
    }

    private void SelectAvatar_Click(object sender, RoutedEventArgs e)
    {
        var path = SelectImageFile("选择头像参考照");
        if (path == null)
            return;

        _avatarSourcePath = path;
        _removeAvatarPhoto = false;
        ApplyPreview(AvatarPreview, path);
        RefreshPromptPreview();
    }

    private void RemoveAvatar_Click(object sender, RoutedEventArgs e)
    {
        _avatarSourcePath = null;
        _removeAvatarPhoto = true;
        ApplyPreview(AvatarPreview, null);
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

    private void RemoveFullBody_Click(object sender, RoutedEventArgs e)
    {
        _fullBodySourcePath = null;
        _removeFullBodyPhoto = true;
        ApplyPreview(FullBodyPreview, null);
        RefreshPromptPreview();
    }

    private void PromptInputChanged(object sender, TextChangedEventArgs e)
    {
        RefreshPromptPreview();
    }

    private void AccountIdentityChanged(object sender, TextChangedEventArgs e)
    {
        RefreshAccountIdentityPreview();
    }

    private void PromptConsentChanged(object sender, RoutedEventArgs e)
    {
        RefreshPromptPreview();
    }

    private void RefreshAccountIdentityPreview()
    {
        var initial = BuildInitial(TxtDisplayName.Text);
        var avatarPath = ResolveAccountAvatarPreviewPath();
        var hasAvatar = !string.IsNullOrWhiteSpace(avatarPath) && File.Exists(avatarPath);

        AccountAvatar.Initial = initial;
        AccountAvatarPreview.Initial = initial;
        TxtSummaryAvatarState.Text = hasAvatar ? "已设置头像" : "使用首字母";

        ApplyAccountAvatarPreview(avatarPath);
        ApplyPreview(AvatarPreview, ResolveProfilePreviewPath(_currentProfile?.AvatarPhotoPath));
        RefreshPromptPreview();
    }

    private void ApplyAccountAvatarPreview()
    {
        ApplyAccountAvatarPreview(ResolveAccountAvatarPreviewPath());
    }

    private void ApplyAccountAvatarPreview(string? avatarPath)
    {
        AccountAvatar.AvatarPath = avatarPath;
        AccountAvatarPreview.AvatarPath = avatarPath;
    }

    private string? ResolveAccountAvatarPreviewPath()
    {
        if (_removeAccountAvatarPhoto)
            return null;

        if (!string.IsNullOrWhiteSpace(_accountAvatarSourcePath))
            return _accountAvatarSourcePath;

        return ResolveStoredImagePath(_effectiveAccountAvatarPath);
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
            TxtProfileDisplayName.Text,
            heightCm,
            TxtBodyShape.Text,
            TxtSkinTone.Text,
            TxtHairLength.Text,
            TxtHairColor.Text,
            TxtFaceFeaturesSummary.Text,
            TxtStyleKeywords.Text,
            TxtAvoidKeywords.Text,
            ResolvePreviewProfileAvatarPath(),
            ResolvePreviewPhotoPath(_removeFullBodyPhoto, _fullBodySourcePath, _currentProfile?.FullBodyPhotoPath),
            ChkConsent.IsChecked == true
                ? _currentProfile?.CloudUploadConsentAcceptedAt ?? DateTime.Now
                : null);
    }

    private string? ResolvePreviewProfileAvatarPath()
    {
        if (_removeAvatarPhoto)
            return null;

        if (!string.IsNullOrWhiteSpace(_avatarSourcePath))
            return _avatarSourcePath;

        if (!string.IsNullOrWhiteSpace(_currentProfile?.AvatarPhotoPath))
            return _currentProfile.AvatarPhotoPath;

        if (_removeAccountAvatarPhoto)
            return null;

        return !string.IsNullOrWhiteSpace(_accountAvatarSourcePath)
            ? _accountAvatarSourcePath
            : _effectiveAccountAvatarPath;
    }

    private static string? ResolvePreviewPhotoPath(bool markedForRemoval, string? selectedSourcePath, string? persistedRelativePath)
    {
        if (markedForRemoval)
            return null;

        if (!string.IsNullOrWhiteSpace(selectedSourcePath))
            return selectedSourcePath;

        return persistedRelativePath;
    }

    private async void SaveAccount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var user = await _localUserService.UpdateAsync(
                _currentUserId,
                TxtDisplayName.Text,
                accountName: TxtAccountName.Text,
                avatarSourcePath: _accountAvatarSourcePath,
                removeAvatarPhoto: _removeAccountAvatarPhoto);

            _effectiveAccountAvatarPath = user.AvatarPhotoPath;
            _accountAvatarSourcePath = null;
            _removeAccountAvatarPhoto = false;
            await _currentUserContext.SetCurrentUserIdAsync(_currentUserId);
            BindUser(user);
            ToastService.Instance.ShowSuccess("账号资料已保存");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("账号资料保存失败", ex.Message);
        }
    }

    private async void SaveProfile_Click(object sender, RoutedEventArgs e)
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

            var saved = await _personalProfileService.SaveAsync(new SavePersonalProfileRequest(
                TxtProfileDisplayName.Text,
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

            _currentProfile = saved;
            _avatarSourcePath = null;
            _fullBodySourcePath = null;
            _removeAvatarPhoto = false;
            _removeFullBodyPhoto = false;
            BindProfile(saved);
            ToastService.Instance.ShowSuccess("个人档案已保存");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("个人档案保存失败", ex.Message);
        }
    }

    private async void SaveSecurity_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _localAuthService.UpdateOwnCredentialAsync(_currentUserId, NewPasswordBox.Password, NewPinBox.Password);
            NewPasswordBox.Clear();
            NewPinBox.Clear();
            ToastService.Instance.ShowSuccess("密码与 PIN 已更新");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("安全设置更新失败", ex.Message);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private static string BuildInitial(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "衣"
            : value.Trim()[0].ToString();
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
