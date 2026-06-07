using ClosetApp.UI.Services;
using System.Windows;
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
            comboBox.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent,
                Source = comboBox
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
            var window = new Window();
            var comboBox = new ComboBox();
            comboBox.Items.Add("A");
            comboBox.Items.Add("B");
            window.Content = comboBox;
            window.Show();
            comboBox.IsDropDownOpen = true;

            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent,
                Source = comboBox
            };

            ComboBoxWheelGuard.HandlePreviewMouseWheel(args);

            Assert.False(args.Handled);
            window.Close();
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
