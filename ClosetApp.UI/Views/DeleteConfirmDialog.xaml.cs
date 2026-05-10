using System.Windows;

namespace ClosetApp.UI.Views;

public partial class DeleteConfirmDialog : Window
{
    public DeleteConfirmDialog(string type, string name)
    {
        InitializeComponent();
        TxtMessage.Text = "确定要删除这个" + type + " " + name + " 吗？此操作无法撤销。";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}