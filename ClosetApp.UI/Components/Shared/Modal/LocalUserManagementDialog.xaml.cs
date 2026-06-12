using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows.Threading;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class LocalUserManagementDialog : UserControl, IModalActivationAware
{
    private readonly ILocalUserService _localUserService;
    private readonly ILocalAuthService _localAuthService;
    private readonly IAiAssetStorageService _assetStorageService;
    private string? _avatarSourcePath;
    private bool _removeAvatarPhoto;
    private List<LocalUserRow> _allRows = [];
    private Guid? _selectedUserId;
    private Guid? _currentUserId;
    private LocalUser? _currentUser;
    private bool _isRefreshing;

    public LocalUserManagementDialog()
    {
        _localUserService = App.Services.GetRequiredService<ILocalUserService>();
        _localAuthService = App.Services.GetRequiredService<ILocalAuthService>();
        _assetStorageService = App.Services.GetRequiredService<IAiAssetStorageService>();
        InitializeComponent();
    }

    public Task OnModalActivatedAsync()
    {
        if (_isRefreshing)
            return Task.CompletedTask;

        return Dispatcher.InvokeAsync(RefreshAsync, DispatcherPriority.Background).Task.Unwrap();
    }

    private async Task RefreshAsync()
    {
        if (_isRefreshing)
            return;

        _isRefreshing = true;
        try
        {
            _avatarSourcePath = null;
            _removeAvatarPhoto = false;
            var currentUser = await _localUserService.GetCurrentAsync();
            if (currentUser.Role != LocalUserRole.SuperAdmin)
            {
                ToastService.Instance.ShowError("无权管理用户", "只有超级管理员可以打开用户管理。");
                ModalService.Instance.Hide();
                return;
            }

            _currentUserId = currentUser.Id;
            _currentUser = currentUser;
            _selectedUserId ??= currentUser.Id;

            _allRows = (await _localUserService.GetAllAsync())
                .Select(user => new LocalUserRow(user, _currentUserId.Value, ResolveAvatarPath(user)))
                .ToList();

            UpdateStats(currentUser);
            ApplyUserFilter();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersList.SelectedItem is LocalUserRow row)
        {
            _selectedUserId = row.Id;
            _avatarSourcePath = null;
            _removeAvatarPhoto = false;
        }
    }

    private void UserSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyUserFilter();
    }

    private void ShowCreateUser_Click(object sender, RoutedEventArgs e)
    {
        CreateUserPanel.Visibility = CreateUserPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;

        if (CreateUserPanel.Visibility == Visibility.Visible)
            TxtNewAccountName.Focus();
    }

    private async void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var user = await _localUserService.CreateMemberAsync(TxtNewAccountName.Text, TxtNewUserName.Text, NewUserPasswordBox.Password, NewUserPinBox.Password);
            _selectedUserId = user.Id;
            TxtNewAccountName.Clear();
            TxtNewUserName.Clear();
            NewUserPasswordBox.Clear();
            NewUserPinBox.Clear();
            CreateUserPanel.Visibility = Visibility.Collapsed;
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("用户已创建");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("创建用户失败", ex.Message);
        }
    }

    private async void SaveUser_Click(object sender, RoutedEventArgs e)
    {
        var row = ResolveTargetRow(sender, preferCurrentUser: false);
        if (row == null)
            return;

        await _localUserService.UpdateAsync(
            row.Id,
            row.EditableName,
            accountName: row.EditableAccountName,
            avatarSourcePath: _avatarSourcePath,
            removeAvatarPhoto: _removeAvatarPhoto);
        _avatarSourcePath = null;
        _removeAvatarPhoto = false;
        await RefreshAsync();
        ToastService.Instance.ShowSuccess("用户信息已保存");
    }

    private async void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not LocalUserRow row || !row.CanDelete)
            return;

        var confirmed = await ConfirmModal.ShowDeleteAsync(
            $"将彻底删除「{row.DisplayName}」的衣物、搭配、标签、穿着记录、效果图和个人档案。");
        if (!confirmed)
            return;

        await _localUserService.DeleteAsync(row.Id);
        _selectedUserId = _currentUserId;
        await RefreshAsync();
        ToastService.Instance.ShowSuccess("用户已删除");
    }

    private async void ResetCredential_Click(object sender, RoutedEventArgs e)
    {
        var row = ResolveTargetRow(sender, preferCurrentUser: false);
        if (row == null || !row.CanResetCredential)
            return;

        try
        {
            if (row.IsCurrent)
                await _localAuthService.UpdateOwnCredentialAsync(row.Id, ResetPasswordBox.Password, ResetPinBox.Password);
            else
                await _localAuthService.ResetMemberCredentialAsync(row.Id, ResetPasswordBox.Password, ResetPinBox.Password);

            ResetPasswordBox.Clear();
            ResetPinBox.Clear();
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("用户凭证已重置");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("重置凭证失败", ex.Message);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private void SelectAvatar_Click(object sender, RoutedEventArgs e)
    {
        var row = ResolveTargetRow(sender, preferCurrentUser: false);
        if (row == null)
            return;

        var path = SelectImageFile("选择用户头像");
        if (path == null)
            return;

        _avatarSourcePath = path;
        _removeAvatarPhoto = false;
        row.AvatarPath = path;

        if (row.IsCurrent)
            ApplyCurrentUserHero(row);
    }

    private void RemoveAvatar_Click(object sender, RoutedEventArgs e)
    {
        if (UsersList.SelectedItem is not LocalUserRow row)
            return;

        _avatarSourcePath = null;
        _removeAvatarPhoto = true;
        row.AvatarPath = null;
    }

    private void ApplyUserFilter()
    {
        var keyword = TxtUserSearch?.Text?.Trim();
        var rows = string.IsNullOrWhiteSpace(keyword)
            ? _allRows
            : _allRows
                .Where(row =>
                    row.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    row.AccountName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .ToList();

        UsersList.ItemsSource = rows;
        UsersList.SelectedItem = rows.FirstOrDefault(row => row.Id == _selectedUserId) ?? rows.FirstOrDefault();
    }

    private void UpdateStats(LocalUser currentUser)
    {
        TxtTotalUsers.Text = _allRows.Count.ToString();
        TxtMemberUsers.Text = _allRows.Count(row => row.User.Role == LocalUserRole.Member).ToString();
        ApplyCurrentUserHero(_allRows.First(row => row.Id == currentUser.Id));
    }

    private LocalUserRow? ResolveTargetRow(object sender, bool preferCurrentUser)
    {
        if (!preferCurrentUser && (sender as FrameworkElement)?.DataContext is LocalUserRow boundRow)
            return boundRow;

        if (_currentUserId == null)
            return null;

        return _allRows.FirstOrDefault(row => row.Id == _currentUserId.Value);
    }

    private void ApplyCurrentUserHero(LocalUserRow row)
    {
        CurrentSessionAvatar.AvatarPath = row.AvatarPath;
        CurrentSessionAvatar.Initial = row.AvatarInitial;
        TxtCurrentSessionUser.Text = $"{row.EditableName} · {row.RoleText}";
        TxtCurrentSessionContext.Text = row.IsCurrent ? "当前登录用户" : row.SessionText;
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

    private string? ResolveAvatarPath(LocalUser user)
    {
        if (string.IsNullOrWhiteSpace(user.AvatarPhotoPath))
            return null;

        return Path.IsPathRooted(user.AvatarPhotoPath)
            ? user.AvatarPhotoPath
            : _assetStorageService.GetProfileReferenceFullPath(user.AvatarPhotoPath, user.Id);
    }

    private sealed class LocalUserRow : INotifyPropertyChanged
    {
        private string? _avatarPath;
        private string _editableAccountName;
        private string _editableName;

        public LocalUserRow(LocalUser user, Guid currentUserId, string? resolvedAvatarPath)
        {
            User = user;
            IsCurrent = user.Id == currentUserId;
            _editableAccountName = user.AccountName;
            _editableName = user.DisplayName;
            _avatarPath = resolvedAvatarPath;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public LocalUser User { get; }
        public Guid Id => User.Id;
        public string AccountName => User.AccountName;
        public string DisplayName => User.DisplayName;
        public string? AvatarPath
        {
            get => _avatarPath;
            set
            {
                if (string.Equals(_avatarPath, value, StringComparison.Ordinal))
                    return;

                _avatarPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasAvatar));
            }
        }
        public bool HasAvatar => !string.IsNullOrWhiteSpace(AvatarPath);
        public string EditableAccountName
        {
            get => _editableAccountName;
            set
            {
                if (string.Equals(_editableAccountName, value, StringComparison.Ordinal))
                    return;

                _editableAccountName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AccountHandle));
            }
        }

        public string EditableName
        {
            get => _editableName;
            set
            {
                if (string.Equals(_editableName, value, StringComparison.Ordinal))
                    return;

                _editableName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AvatarInitial));
                OnPropertyChanged(nameof(SummaryText));
            }
        }
        public bool IsCurrent { get; }
        public bool HasSelection => true;
        public bool CanDelete => User.Role != LocalUserRole.SuperAdmin;
        public bool CanResetCredential => IsCurrent || User.Role != LocalUserRole.SuperAdmin;
        public string RoleText => User.Role == LocalUserRole.SuperAdmin ? "超级管理员" : "普通用户";
        public string AvatarInitial => string.IsNullOrWhiteSpace(EditableName) ? "衣" : EditableName.Trim()[0].ToString();
        public string SummaryText => $"{RoleText}{(IsCurrent ? " · 当前" : string.Empty)}";
        public string AccountHint => $"账号 {AccountName}";
        public string ListBadgeText => IsCurrent ? "当前" : User.Role == LocalUserRole.SuperAdmin ? "管理员" : string.Empty;
        public string AccountHandle => $"@{EditableAccountName}";
        public string SessionText => IsCurrent ? "当前会话" : "可独立登录";
        public string CredentialSummary
        {
            get
            {
                if (User.HasPasswordCredential && User.HasPinCredential)
                    return "密码 + PIN";

                if (User.HasPasswordCredential)
                    return "仅密码";

                if (User.HasPinCredential)
                    return "仅 PIN";

                return "未设置凭证";
            }
        }
        public string ActivityText => $"{BuildLastLoginText()} · 创建于 {FormatDate(User.CreatedAt)}";

        public string RuleText => User.Role == LocalUserRole.SuperAdmin
            ? "超级管理员用于管理本机所有本地用户，不能删除。可在这里更新当前管理员账号、密码或 PIN。"
            : "普通用户拥有独立衣柜、搭配、标签、穿着记录、效果图和个人档案。删除用户会同时删除该用户的全部本地数据。";
        public string DangerHintText => User.Role == LocalUserRole.SuperAdmin
            ? "超级管理员账号不能删除。建议先保存资料或更新凭证。"
            : "删除后将同时移除该用户的衣柜、搭配、标签、穿着记录、效果图与个人档案，且无法恢复。";

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string BuildLastLoginText()
        {
            return User.LastLoginAt.HasValue
                ? $"最近登录 {FormatDate(User.LastLoginAt.Value)}"
                : "还没有登录记录";
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToString("yyyy.MM.dd");
        }
    }
}
