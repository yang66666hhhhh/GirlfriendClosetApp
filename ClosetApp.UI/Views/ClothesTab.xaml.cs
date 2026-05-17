using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components;
using ClosetApp.UI.Components.Clothing;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class ClothesTab : UserControl
{
    private readonly WardrobeViewModel _viewModel;

    public ClothesTab()
    {
        _viewModel = App.Services.GetRequiredService<WardrobeViewModel>();
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(WardrobeViewModel.FilteredClothes) or nameof(WardrobeViewModel.IsEmpty))
                Dispatcher.BeginInvoke(UpdateCardWidth, System.Windows.Threading.DispatcherPriority.Loaded);
        };
        Loaded += (s, e) => _ = LoadClothesAsync();
        SizeChanged += (_, _) => UpdateCardWidth();
    }

    private async Task LoadClothesAsync()
    {
        await _viewModel.LoadClothesAsync();
        Dispatcher.BeginInvoke(UpdateCardWidth, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateCardWidth()
    {
        if (ClothesList == null || ContentScroller == null) return;
        var masonry = FindVisualChild<MasonryPanel>(ClothesList);
        if (masonry == null) return;

        double availWidth = ContentScroller.ActualWidth - 128;
        if (availWidth <= 0) return;

        double gap = 20;
        int cols = (int)((availWidth + gap) / (260 + gap));
        cols = Math.Max(1, cols);

        double totalGap = gap * (cols - 1);
        double cardWidth = Math.Floor((availWidth - totalGap) / cols);
        cardWidth = Math.Clamp(cardWidth, 240, 300);

        masonry.ColumnWidth = cardWidth;
        masonry.Spacing = gap;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void ToggleFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleFilterExpanded();
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearFilters();
    }

    private void AddClothing_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new ClothingEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await _viewModel.AddClothingAsync(result.Entity!);
        });
    }

    private void ClothingCard_Edit(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var oldImagePath = clothing.ImagePath;
        EditorModal.Show(new ClothingEditorPanel(clothing), async result =>
        {
            if (result.Type == EditorResultType.Saved)
            {
                await _viewModel.UpdateClothingAsync(result.Entity!, oldImagePath);
            }
            else if (result.Type == EditorResultType.Deleted)
            {
                await _viewModel.DeleteClothingAsync(clothing);
            }
        });
    }

    private async void ClothingCard_Delete(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not Clothing clothing) return;

        var confirmed = await ShowDeleteConfirmAsync($"确定删除「{clothing.Name}」吗？");
        if (!confirmed)
            return;

        await _viewModel.DeleteClothingAsync(clothing);
    }

    private static async Task<bool> ShowDeleteConfirmAsync(string detail)
    {
        var dialog = new ConfirmDialog
        {
            Title = "删除衣服",
            Body = "删除后将无法恢复。",
            Detail = detail,
            ConfirmText = "删除",
            CancelText = "取消"
        };

        var tcs = new TaskCompletionSource<bool>();
        void ConfirmedHandler(object? sender, EventArgs e) => tcs.TrySetResult(true);
        void CancelledHandler(object? sender, EventArgs e) => tcs.TrySetResult(false);
        dialog.Confirmed += ConfirmedHandler;
        dialog.Cancelled += CancelledHandler;
        ModalService.Instance.Show(dialog);
        var result = await tcs.Task;
        ModalService.Instance.Hide();
        return result;
    }

}
