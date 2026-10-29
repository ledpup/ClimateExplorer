namespace ClimateExplorer.Web.UnitTests;

using System;
using System.IO;

/// <summary>
/// Finds project folders in the source tree, for tests that scan source files rather than run code.
/// </summary>
internal static class SourceTree
{
    public static string FindProjectRoot(string projectName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            // The project folder, not a build output folder of the same name.
            var candidate = Path.Combine(directory.FullName, projectName);

            if (File.Exists(Path.Combine(candidate, projectName + ".csproj")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Couldn't find {projectName} above {AppContext.BaseDirectory}");
    }
}
