using System.Windows;
using System.Windows.Controls;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Views;

public partial class AddTagDialog : Window
{
    public Tag? Result { get; private set; }

    public AddTagDialog()
    {
        InitializeComponent();
        CmbColor.SelectedIndex = 0;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("请输入标签名称", "提示");
            return;
        }

        var color = (CmbColor.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "#667eea";

        Result = new Tag
        {
            Name = TxtName.Text,
            Color = color
        };

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}