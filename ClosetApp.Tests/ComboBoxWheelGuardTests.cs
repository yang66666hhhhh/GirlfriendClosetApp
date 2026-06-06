using ClosetApp.UI.Services;
using System.Windows.Controls;
using System.Windows.Input;
using Xunit;

namespace ClosetApp.Tests;

public class ComboBoxWheelGuardTests
{
    [Fact]
    public void HandlePreviewMouseWheel_WhenComboBoxClosed_MarksHandled()
    {
        RunOnStaThread(() =>
        {
            var comboBox = new ComboBox();
            comboBox.Items.Add("A");
            comboBox.Items.Add("B");
            comboBox.SelectedIndex = 0;

            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent,
                Source = comboBox,
                OriginalSource = comboBox
            };

            ComboBoxWheelGuard.HandlePreviewMouseWheel(args);

            Assert.True(args.Handled);
            Assert.Equal(0, comboBox.SelectedIndex);
        });
    }

    [Fact]
    public void HandlePreviewMouseWheel_WhenComboBoxOpen_DoesNotHandle()
    {
        RunOnStaThread(() =>
        {
            var comboBox = new ComboBox();
            comboBox.Items.Add("A");
            comboBox.Items.Add("B");
            comboBox.IsDropDownOpen = true;

            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent,
                Source = comboBox,
                OriginalSource = comboBox
            };

            ComboBoxWheelGuard.HandlePreviewMouseWheel(args);

            Assert.False(args.Handled);
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }
    }
}
