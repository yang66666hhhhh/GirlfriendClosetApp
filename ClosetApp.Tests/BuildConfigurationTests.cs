using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class BuildConfigurationTests
{
    [Fact]
    public void DirectoryBuildProps_ExcludesArtifactsFromDefaultCompileItems()
    {
        var propsPath = FindProjectFile("Directory.Build.props");
        var content = File.ReadAllText(propsPath);

        Assert.Contains("DefaultItemExcludes", content);
        Assert.Contains("artifacts\\**", content);
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
