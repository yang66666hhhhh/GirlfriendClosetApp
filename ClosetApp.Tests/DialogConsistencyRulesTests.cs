using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClosetApp.Tests;

public class DialogConsistencyRulesTests
{
    [Fact]
    public void UiProject_OnlyKeepsRootWindowsAsWindowTypes()
    {
        var csFiles = Directory
            .EnumerateFiles(FindProjectDirectory("ClosetApp.UI"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var allowedClasses = new HashSet<string>(StringComparer.Ordinal)
        {
            "MainWindow",
            "LoginWindow"
        };

        var offendingClasses = new List<string>();

        foreach (var file in csFiles)
        {
            var code = File.ReadAllText(file);
            var matches = Regex.Matches(
                code,
                @"partial\s+class\s+(?<name>\w+)\s*:\s*Window\b",
                RegexOptions.CultureInvariant);

            foreach (Match match in matches)
            {
                var className = match.Groups["name"].Value;
                if (!allowedClasses.Contains(className))
                    offendingClasses.Add($"{Path.GetFileName(file)} => {className}");
            }
        }

        Assert.True(
            offendingClasses.Count == 0,
            "UI 项目不应继续新增普通业务 Window，除应用根窗口外请统一走共享 Modal 体系：" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offendingClasses));
    }

    [Fact]
    public void UiProject_OnlyKeepsRootWindowXamlFiles()
    {
        var xamlFiles = Directory
            .EnumerateFiles(FindProjectDirectory("ClosetApp.UI"), "*.xaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var allowedXamlFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MainWindow.xaml",
            "LoginWindow.xaml"
        };

        var offendingFiles = new List<string>();

        foreach (var file in xamlFiles)
        {
            var xaml = File.ReadAllText(file);
            if (!Regex.IsMatch(xaml, @"^\s*<Window\b", RegexOptions.CultureInvariant))
                continue;

            if (!allowedXamlFiles.Contains(Path.GetFileName(file)))
                offendingFiles.Add(Path.GetRelativePath(FindProjectDirectory("ClosetApp.UI"), file));
        }

        Assert.True(
            offendingFiles.Count == 0,
            "业务弹窗不应继续使用 Window 根节点，请统一迁移到共享 Modal 体系：" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offendingFiles));
    }

    [Fact]
    public void UiProject_MessageBoxOnlyExistsInAppLevelFallback()
    {
        var csFiles = Directory
            .EnumerateFiles(FindProjectDirectory("ClosetApp.UI"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var offendingFiles = new List<string>();

        foreach (var file in csFiles)
        {
            var code = File.ReadAllText(file);
            if (!code.Contains("MessageBox.Show", StringComparison.Ordinal))
                continue;

            if (!string.Equals(Path.GetFileName(file), "App.xaml.cs", StringComparison.OrdinalIgnoreCase))
                offendingFiles.Add(Path.GetRelativePath(FindProjectDirectory("ClosetApp.UI"), file));
        }

        Assert.True(
            offendingFiles.Count == 0,
            "业务流程不应继续直接使用 MessageBox，常规确认请统一走 ConfirmModal 或共享 Modal：" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offendingFiles));
    }

    [Fact]
    public void ConfirmModal_SupportsSingleActionMessageDialogs()
    {
        var modalCode = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/ConfirmModal.cs"));
        var dialogCode = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/ConfirmDialog.xaml.cs"));

        Assert.Contains("ShowMessageAsync", modalCode);
        Assert.Contains("showCancel: false", modalCode);
        Assert.Contains("confirmStyleKey = \"ModalSaveButton\"", modalCode);
        Assert.Contains("public bool IsCancelVisible", dialogCode);
        Assert.Contains("CancelButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;", dialogCode);
    }

    private static string FindProjectDirectory(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Cannot locate {relativePath} from {AppContext.BaseDirectory}.");
    }

    private static string FindProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Cannot locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
