using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class LocalUserManagementDialog : UserControl
{
    private readonly ILocalUserService _localUserService;
    private readonly ILocalAuthService _localAuthService;
    private List<LocalUserRow> _allRows = [];
    private Guid? _selectedUserId;
    private Guid? _currentUserId;

    public LocalUserManagementDialog()
    {
        _localUserService = App.Services.GetRequiredService<ILocalUserService>();
        _localAuthService = App.Services.GetRequiredService<ILocalAuthService>();
        InitializeComponent();
        Loaded += LocalUserManagementDialog_Loaded;
    }

    private async void LocalUserManagementDialog_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var currentUser = await _localUserService.GetCurrentAsync();
        if (currentUser.Role != LocalUserRole.SuperAdmin)
        {
            ToastService.Instance.ShowError("无权管理用户", "只有超级管理员可以打开用户管理。");
            ModalService.Instance.Hide();
            return;
        }

        _currentUserId = currentUser.Id;
        _selectedUserId ??= currentUser.Id;

        _allRows = (await _localUserService.GetAllAsync())
            .Select(user => new LocalUserRow(user, _currentUserId.Value))
            .ToList();

        UpdateStats(currentUser);
        ApplyUserFilter();
    }

    private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersList.SelectedItem is LocalUserRow row)
            _selectedUserId = row.Id;
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
        if ((sender as FrameworkElement)?.DataContext is not LocalUserRow row)
            return;

        await _localUserService.UpdateAsync(row.Id, row.EditableName, accountName: row.EditableAccountName);
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
        if ((sender as FrameworkElement)?.DataContext is not LocalUserRow row || !row.CanResetCredential)
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
        TxtCurrentSession.Text = currentUser.DisplayName;
    }

    private sealed class LocalUserRow
    {
        public LocalUserRow(LocalUser user, Guid currentUserId)
        {
            User = user;
            IsCurrent = user.Id == currentUserId;
            EditableAccountName = user.AccountName;
            EditableName = user.DisplayName;
        }

        public LocalUser User { get; }
        public Guid Id => User.Id;
        public string AccountName => User.AccountName;
        public string DisplayName => User.DisplayName;
        public string? AvatarPath => User.AvatarPhotoPath;
        public string EditableAccountName { get; set; }
        public string EditableName { get; set; }
        public bool IsCurrent { get; }
        public bool HasSelection => true;
        public bool CanDelete => User.Role != LocalUserRole.SuperAdmin;
        public bool CanResetCredential => IsCurrent || User.Role != LocalUserRole.SuperAdmin;
        public string AvatarInitial => string.IsNullOrWhiteSpace(DisplayName) ? "衣" : DisplayName.Trim()[0].ToString();
        public string SummaryText => $"{RoleText}{(IsCurrent ? " · 当前" : string.Empty)}";
        public string AccountHint => $"账号 {AccountName}";

        public string RuleText => User.Role == LocalUserRole.SuperAdmin
            ? "超级管理员用于管理本机所有本地用户，不能删除。可在这里更新当前管理员账号、密码或 PIN。"
            : "普通用户拥有独立衣柜、搭配、标签、穿着记录、效果图和个人档案。删除用户会同时删除该用户的全部本地数据。";

        private string RoleText => User.Role == LocalUserRole.SuperAdmin ? "超级管理员" : "普通用户";
    }
}
