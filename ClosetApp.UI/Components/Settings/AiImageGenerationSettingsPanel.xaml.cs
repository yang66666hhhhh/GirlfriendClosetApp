using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class AiImageGenerationSettingsPanel : UserControl
{
    private sealed record AiConfigPreset(string Key, string BaseUrl, string Model, int TimeoutSeconds, string SuccessMessage);

    private static readonly IReadOnlyDictionary<string, AiConfigPreset> Presets =
        new Dictionary<string, AiConfigPreset>(StringComparer.OrdinalIgnoreCase)
        {
            ["openai-main"] = new("openai-main", "https://api.openai.com/v1", "gpt-image-2", 180, "已切换到 GPT Image 2 图片配置"),
            ["openai-1_5"] = new("openai-1_5", "https://api.openai.com/v1", "gpt-image-1.5", 120, "已切换到 OpenAI 1.5 图片配置"),
            ["openai-mini"] = new("openai-mini", "https://api.openai.com/v1", "gpt-image-1-mini", 90, "已切换到 OpenAI Mini 图片配置"),
            ["openai-gpt55"] = new("openai-gpt55", "https://api.openai.com/v1", "gpt-5.5", 180, "已切换到 GPT-5.5 兼容实验配置")
        };

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
        EditorModal.Show(new PersonalProfileEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                await _viewModel.RefreshAiGenerationSettingsAsync();
            }
        });
    }

    public async Task RefreshAsync()
    {
        var preferences = await _preferencesService.GetAsync();
        TxtBaseUrl.Text = preferences.BaseUrl;
        TxtModel.Text = preferences.Model;
        TxtTimeoutSeconds.Text = preferences.TimeoutSeconds.ToString();
        ApplyModelPresetSelection(preferences.Model);
        ApplyPresetHighlight(preferences);
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

    private async void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        if (sender is not Button button)
            return;

        var presetKey = button.Name switch
        {
            nameof(BtnPresetOpenAiMain) => "openai-main",
            nameof(BtnPresetOpenAi15) => "openai-1_5",
            nameof(BtnPresetOpenAiMini) => "openai-mini",
            nameof(BtnPresetOpenAi55) => "openai-gpt55",
            _ => string.Empty
        };

        if (!Presets.TryGetValue(presetKey, out var preset))
            return;

        try
        {
            SetBusyState(true, "正在切换 AI 配置...");
            var previousPreferences = await _preferencesService.GetAsync();
            TxtBaseUrl.Text = preset.BaseUrl;
            TxtModel.Text = preset.Model;
            TxtTimeoutSeconds.Text = preset.TimeoutSeconds.ToString();
            ApplyModelPresetSelection(preset.Model);
            await PersistCurrentInputsAsync();

            var hasApiKey = !string.IsNullOrWhiteSpace(await _preferencesService.GetApiKeyAsync());
            if (hasApiKey)
            {
                try
                {
                    await _viewModel.TestAiConnectionAsync();
                    await RefreshAsync();
                    ToastService.Instance.ShowSuccess($"{preset.SuccessMessage}，接口连通性测试通过");
                }
                catch (Exception testEx)
                {
                    TxtBaseUrl.Text = previousPreferences.BaseUrl;
                    TxtModel.Text = previousPreferences.Model;
                    TxtTimeoutSeconds.Text = previousPreferences.TimeoutSeconds.ToString();
                    ApplyModelPresetSelection(previousPreferences.Model);
                    await PersistCurrentInputsAsync();
                    await RefreshAsync();
                    ToastService.Instance.ShowError("切换后连接失败，已回滚", testEx.Message);
                }
            }
            else
            {
                await RefreshAsync();
                ToastService.Instance.ShowSuccess($"{preset.SuccessMessage}，请再保存 API Key 后测试连接");
            }
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("切换 AI 配置失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
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

        BtnPresetOpenAiMain.IsEnabled = !isBusy;
        BtnPresetOpenAi15.IsEnabled = !isBusy;
        BtnPresetOpenAiMini.IsEnabled = !isBusy;
        BtnPresetOpenAi55.IsEnabled = !isBusy;
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

        if (!string.Equals(item.Tag?.ToString(), "__custom__", StringComparison.Ordinal))
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
            return;
        }

        CmbModelPreset.SelectedItem = CmbModelPreset.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), "__custom__", StringComparison.Ordinal));
    }

    private void ApplyPresetHighlight(AiGenerationPreferences preferences)
    {
        SetPresetState(BtnPresetOpenAiMain, TxtPresetOpenAiMain, IsPresetActive(preferences, "openai-main"));
        SetPresetState(BtnPresetOpenAi15, TxtPresetOpenAi15, IsPresetActive(preferences, "openai-1_5"));
        SetPresetState(BtnPresetOpenAiMini, TxtPresetOpenAiMini, IsPresetActive(preferences, "openai-mini"));
        SetPresetState(BtnPresetOpenAi55, TxtPresetOpenAi55, IsPresetActive(preferences, "openai-gpt55"));
    }

    private static bool IsPresetActive(AiGenerationPreferences preferences, string presetKey)
    {
        return Presets.TryGetValue(presetKey, out var preset)
               && string.Equals(preferences.BaseUrl, preset.BaseUrl, StringComparison.OrdinalIgnoreCase)
               && string.Equals(preferences.Model, preset.Model, StringComparison.OrdinalIgnoreCase)
               && preferences.TimeoutSeconds == preset.TimeoutSeconds;
    }

    private void SetPresetState(Button button, TextBlock title, bool isActive)
    {
        button.Tag = isActive ? "active" : null;
        title.Text = isActive ? $"{title.Text.Split('（')[0].Trim()}（当前）" : title.Text.Split('（')[0].Trim();
    }
}
