using System.Windows;
using System.Windows.Controls;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class ConfirmDialog : UserControl
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    public string Body
    {
        get => BodyText.Text;
        set => BodyText.Text = value;
    }

    public string Detail
    {
        get => DetailText.Text;
        set => DetailText.Text = value;
    }

    public string ConfirmText
    {
        get => (string)ConfirmButton.Content;
        set => ConfirmButton.Content = value;
    }

    public string CancelText
    {
        get => (string)CancelButton.Content;
        set => CancelButton.Content = value;
    }

    public event EventHandler? Confirmed;
    public event EventHandler? Cancelled;

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => Confirmed?.Invoke(this, EventArgs.Empty);
    private void CancelButton_Click(object sender, RoutedEventArgs e) => Cancelled?.Invoke(this, EventArgs.Empty);
}
