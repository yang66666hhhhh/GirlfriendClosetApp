using ClosetApp.UI.Services;
using System.Windows.Controls;
using Xunit;

namespace ClosetApp.Tests;

public class ModalServiceTests
{
    [Fact]
    public void Hide_WithStackedModal_RevealsPreviousContent()
    {
        RunOnStaThread(() =>
        {
            var service = new ModalService();
            var shown = new List<object?>();
            var hideCount = 0;

            service.ModalShowRequested += content => shown.Add(content);
            service.ModalHideRequested += () => hideCount++;

            var parent = new UserControl();
            var confirm = new UserControl();

            service.Show(parent);
            service.Show(confirm);
            service.Hide();
            service.Hide();

            Assert.Equal(3, shown.Count);
            Assert.Same(parent, shown[0]);
            Assert.Same(confirm, shown[1]);
            Assert.Same(parent, shown[2]);
            Assert.Equal(1, hideCount);
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
            throw exception;
    }
}
