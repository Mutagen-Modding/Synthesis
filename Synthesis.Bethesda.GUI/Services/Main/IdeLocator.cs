using System.IO.Abstractions;
using Microsoft.Win32;
using Serilog;
using Path = System.IO.Path;
using SearchOption = System.IO.SearchOption;

namespace Synthesis.Bethesda.GUI.Services.Main;

public interface IIdeLocator
{
    string? RiderPath { get; }
    string? VSPath { get; }
}

public class IdeLocator : IIdeLocator
{
    public string? RiderPath { get; }
    public string? VSPath { get; }

    public IdeLocator(
        ILogger logger,
        IFileSystem fileSystem)
    {
        #region Visual Studio

        try
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var vsMainDir = Path.Combine(programFiles, "Microsoft Visual Studio");
            List<int>? versions = fileSystem.Directory
                .EnumerateDirectories(vsMainDir, "*", SearchOption.TopDirectoryOnly)
                .Select(x =>
                {
                    ReadOnlySpan<char> span = x.AsSpan(vsMainDir[^1] == Path.PathSeparator
                        ? vsMainDir.Length
                        : vsMainDir.Length + 1);
                    if (int.TryParse(span.ToString(), out var y))
                        return y;
                    return -1;
                })
                .Where(x => x != -1)
                .ToList();
            var currentVersion = versions.Max();
            //TODO: assuming Community Version for now
            var exePath = Path.Combine(vsMainDir, $"{currentVersion}", "Community", "Common7", "IDE", "devenv.exe");
            if (fileSystem.File.Exists(exePath))
                VSPath = exePath;
        }
        catch (Exception)
        {
            logger.Information("Could not locate Visual Studio installation.");
        }

        #endregion

        #region Rider
        try
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var defaultRiderLocation = Path.Combine(programFiles, "JetBrains", "JetBrains Rider", "bin");
            if (fileSystem.Directory.Exists(defaultRiderLocation))
            {
                var exePath = Path.Combine(defaultRiderLocation, "rider64.exe");
                if(fileSystem.File.Exists(exePath))
                    RiderPath = exePath;
            }
            else
            {
                const string toolBoxRegKey = @"Software\JetBrains\Toolbox";
                var toolBoxKey = Registry.CurrentUser.OpenSubKey(toolBoxRegKey);
                var toolboxBinPath = toolBoxKey?.GetValue("");
                if (toolboxBinPath == null)
                    return;

                var toolBoxPath = Path.GetDirectoryName(toolboxBinPath.ToString());

                const string riderVersionRegKey = @"Software\JetBrains\Rider";
                var riderVersionKey = Registry.CurrentUser.OpenSubKey(riderVersionRegKey);
                var riderVersion = riderVersionKey?.GetValue("");
                if (riderVersion == null)
                    return;

                var exePath = Path.Combine(toolBoxPath ?? string.Empty, "apps", "Rider", "ch-0",
                    riderVersion.ToString() ?? string.Empty, "bin", "rider64.exe");

                if(fileSystem.File.Exists(exePath))
                    RiderPath = exePath;
            }
        }
        catch (Exception)
        {
            logger.Information("Could not locate Rider installation.");
        }

        #endregion
    }
}