using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.Logic.Components.Clothing;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Clothing;

public partial class BatchClothingImportSummaryDialog : UserControl
{
    private readonly Action _jumpToRecentlyImported;

    public BatchClothingImportSummaryDialog(
        BatchClothingImportSummary summary,
        Action jumpToRecentlyImported)
    {
        InitializeComponent();
        _jumpToRecentlyImported = jumpToRecentlyImported;

        TitleText.Text = "导入完成";
        SubtitleText.Text = $"成功导入 {summary.ImportedCount} 件衣服，已经帮你切到“刚导入”筛选。";
        SummaryText.Text = $"这批一共导入了 {summary.ImportedCount} 件";
        SummaryDetailText.Text = summary.HasAnyFollowUp
            ? "下面这些衣服还值得顺手补一下资料，整理完会更好找。"
            : "这批资料已经很完整，直接去“刚导入”里继续看就行。";

        PerfectStatePanel.Visibility = summary.HasAnyFollowUp ? Visibility.Collapsed : Visibility.Visible;

        ConfigureSection(UnnamedSection, UnnamedTitle, UnnamedList, "未命名", summary.UnnamedItems);
        ConfigureSection(UncategorizedSection, UncategorizedTitle, UncategorizedList, "缺分类", summary.UncategorizedItems);
        ConfigureSection(UnseasonedSection, UnseasonedTitle, UnseasonedList, "缺季节", summary.UnseasonedItems);
    }

    private static void ConfigureSection(
        FrameworkElement section,
        TextBlock title,
        ItemsControl list,
        string label,
        IReadOnlyList<BatchClothingImportSummaryItem> items)
    {
        section.Visibility = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        title.Text = $"{label} {items.Count} 件";
        list.ItemsSource = items;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private void JumpButton_Click(object sender, RoutedEventArgs e)
    {
        _jumpToRecentlyImported();
        ModalService.Instance.Hide();
    }
}
