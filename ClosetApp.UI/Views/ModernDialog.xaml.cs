using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClosetApp.UI.Views;

public enum ModernDialogResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

public partial class ModernDialog : Window
{
    public ModernDialogResult Result { get; private set; } = ModernDialogResult.Cancel;

    public ModernDialog(string title, string message, string? okText = "确定", string? cancelText = null, string? yesText = null, string? noText = null)
    {
        InitializeComponent();

        TitleBlock.Text = title;
        MessageBlock.Text = message;
        MessageBlock.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;

        var btnCount = 0;
        if (!string.IsNullOrEmpty(okText))
            btnCount++;
        if (!string.IsNullOrEmpty(cancelText))
            btnCount++;
        if (!string.IsNullOrEmpty(yesText))
            btnCount++;
        if (!string.IsNullOrEmpty(noText))
            btnCount++;

        if (btnCount <= 1)
            ButtonPanel.FlowDirection = FlowDirection.RightToLeft;

        if (!string.IsNullOrEmpty(noText))
            AddButton(noText, ModernDialogResult.No, "DialogDangerButton");
        if (!string.IsNullOrEmpty(yesText))
            AddButton(yesText, ModernDialogResult.Yes, "DialogPrimaryButton");
        if (!string.IsNullOrEmpty(cancelText))
            AddButton(cancelText, ModernDialogResult.Cancel, "DialogSecondaryButton");
        if (!string.IsNullOrEmpty(okText))
            AddButton(okText, ModernDialogResult.OK, "DialogPrimaryButton");

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Result = ModernDialogResult.Cancel;
                CloseWithAnimation();
            }
            else if (e.Key == Key.Enter)
            {
                var okBtn = ButtonPanel.Children.OfType<Button>().LastOrDefault();
                if (okBtn != null)
                    okBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        };

        Owner = System.Windows.Application.Current.MainWindow;
    }

    private void AddButton(string text, ModernDialogResult result, string styleKey)
    {
        var style = (Style)FindResource(styleKey);
        var btn = new Button
        {
            Content = text,
            Style = style,
            Tag = result
        };
        btn.Click += Button_Click;
        ButtonPanel.Children.Add(btn);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ModernDialogResult result)
            Result = result;
        CloseWithAnimation();
    }

    private void Overlay_Click(object sender, MouseButtonEventArgs e)
    {
        Result = ModernDialogResult.Cancel;
        CloseWithAnimation();
    }

    private void CloseWithAnimation()
    {
        var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (s, e) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (Opacity > 0)
        {
            e.Cancel = true;
            CloseWithAnimation();
        }
    }

    public static ModernDialogResult Show(string title, string message, string? okText = "确定", string? cancelText = null, string? yesText = null, string? noText = null)
    {
        var dialog = new ModernDialog(title, message, okText, cancelText, yesText, noText);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        dialog.ShowDialog();
        return dialog.Result;
    }

    public static void ShowInfo(string message, string title = "提示")
    {
        Show(title, message, okText: "好的");
    }

    public static void ShowError(string message, string title = "出错了")
    {
        Show(title, message, okText: "知道了");
    }

    public static ModernDialogResult ShowConfirm(string message, string title = "确认", string confirmText = "确定", string cancelText = "取消")
    {
        return Show(title, message, okText: confirmText, cancelText: cancelText);
    }
}