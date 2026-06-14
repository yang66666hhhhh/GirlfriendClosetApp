using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class AiImageGenerationSettingsPanel : UserControl
{
    private readonly SettingsViewModel _viewModel;
    private readonly IAiGenerationPreferencesService _preferencesService;
    private bool _isShowingApiKey;
    private bool _isSyncingApiKey;
    private bool _isBusy;

    public AiImageGenerationSettingsPanel()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        _preferencesService = App.Services.GetRequiredService<IAiGenerationPreferencesService>();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在保存 AI 配置...");
            await PersistCurrentInputsAsync();
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("AI 配置已保存");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("AI 配置保存失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在测试接口连通性...");
            await PersistCurrentInputsAsync();
            await RefreshAsync();
            await _viewModel.TestAiConnectionAsync();
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("接口连通性测试通过", "当前配置至少可以访问模型列表；图片生成能力仍取决于中转和上游是否真正支持。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("连接测试失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void OpenProfile_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.ShowCached<PersonalCenterDialog>();
    }

    public async Task RefreshAsync()
    {
        var preferences = await _preferencesService.GetAsync();
        TxtBaseUrl.Text = preferences.BaseUrl;
        TxtModel.Text = preferences.Model;
        TxtTimeoutSeconds.Text = preferences.TimeoutSeconds.ToString();
        ApplyModelPresetSelection(preferences.Model);
        await LoadApiKeyAsync();
    }

    private async Task LoadApiKeyAsync()
    {
        var apiKey = await _preferencesService.GetApiKeyAsync() ?? string.Empty;
        SyncApiKeyInputs(apiKey);
    }

    private async Task PersistCurrentInputsAsync()
    {
        if (!int.TryParse(TxtTimeoutSeconds.Text, out var timeoutSeconds))
            timeoutSeconds = 60;

        await _preferencesService.SaveAsync(new SaveAiGenerationPreferencesRequest(
            TxtBaseUrl.Text,
            TxtModel.Text,
            timeoutSeconds,
            GetCurrentApiKeyValue()));

        await _viewModel.RefreshAiGenerationSettingsAsync();
    }

    private void ToggleApiKeyVisibility_Click(object sender, RoutedEventArgs e)
    {
        _isShowingApiKey = !_isShowingApiKey;
        ApplyApiKeyVisibility();
    }

    private void ApiKeyPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncingApiKey || _isShowingApiKey)
            return;

        SyncVisibleText(TxtApiKey.Password);
    }

    private void ApiKeyVisible_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncingApiKey || !_isShowingApiKey)
            return;

        SyncHiddenPassword(TxtApiKeyVisible.Text);
    }

    private void SyncApiKeyInputs(string value)
    {
        _isSyncingApiKey = true;
        TxtApiKey.Password = value;
        TxtApiKeyVisible.Text = value;
        _isSyncingApiKey = false;
        ApplyApiKeyVisibility();
    }

    private void SyncVisibleText(string value)
    {
        _isSyncingApiKey = true;
        TxtApiKeyVisible.Text = value;
        _isSyncingApiKey = false;
    }

    private void SyncHiddenPassword(string value)
    {
        _isSyncingApiKey = true;
        TxtApiKey.Password = value;
        _isSyncingApiKey = false;
    }

    private void ApplyApiKeyVisibility()
    {
        TxtApiKey.Visibility = _isShowingApiKey ? Visibility.Collapsed : Visibility.Visible;
        TxtApiKeyVisible.Visibility = _isShowingApiKey ? Visibility.Visible : Visibility.Collapsed;
        BtnToggleApiKeyVisibility.Content = _isShowingApiKey ? "🙈" : "👁";
        BtnToggleApiKeyVisibility.ToolTip = _isShowingApiKey ? "隐藏 API Key" : "显示 API Key";
    }

    private void SetBusyState(bool isBusy, string? statusText = null)
    {
        _isBusy = isBusy;

        BtnSaveConfig.IsEnabled = !isBusy;
        BtnTestConnection.IsEnabled = !isBusy;
        BtnOpenProfile.IsEnabled = !isBusy;
        BtnToggleApiKeyVisibility.IsEnabled = !isBusy;

        TxtAiOperationStatus.Text = statusText ?? string.Empty;
        TxtAiOperationStatus.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
    }

    private string GetCurrentApiKeyValue()
    {
        return _isShowingApiKey ? TxtApiKeyVisible.Text : TxtApiKey.Password;
    }

    private void ModelPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbModelPreset.SelectedItem is not ComboBoxItem item)
            return;

        var isCustom = string.Equals(item.Tag?.ToString(), "__custom__", StringComparison.Ordinal);
        AiCustomModelPanel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;

        if (!isCustom)
            TxtModel.Text = item.Tag?.ToString() ?? TxtModel.Text;
    }

    private void ApplyModelPresetSelection(string model)
    {
        var matchedItem = CmbModelPreset.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), model, StringComparison.OrdinalIgnoreCase));

        if (matchedItem != null)
        {
            CmbModelPreset.SelectedItem = matchedItem;
            AiCustomModelPanel.Visibility = Visibility.Collapsed;
            return;
        }

        CmbModelPreset.SelectedItem = CmbModelPreset.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), "__custom__", StringComparison.Ordinal));
        AiCustomModelPanel.Visibility = Visibility.Visible;
    }
}
