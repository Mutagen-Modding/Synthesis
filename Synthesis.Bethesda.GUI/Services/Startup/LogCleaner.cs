using System.IO.Abstractions;
using Noggog;
using Noggog.Time;
using Serilog;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.GUI.Logging;

namespace Synthesis.Bethesda.GUI.Services.Startup;

public class LogCleaner : IStartupTask
{
    private const int DaysToKeep = 7;
    private readonly IFileSystem _fileSystem;
    public INowProvider NowProvider { get; }
    private readonly ILogger _logger;
        
    public ILogSettings LogSettings { get; }

    public LogCleaner(
        IFileSystem fileSystem,
        INowProvider nowProvider,
        ILogSettings logSettings,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        NowProvider = nowProvider;
        LogSettings = logSettings;
        _logger = logger;
    }
        
    public void Start()
    {
        foreach (var dir in _fileSystem.Directory.EnumerateDirectoryPaths(LogSettings.LogFolder, includeSelf: false, recursive: false))
        {
            if (!DateTime.TryParseExact(dir.Name, LogSettings.DateFormat, default, default, out var dt)) continue;
            if ((NowProvider.NowLocal - dt).TotalDays > DaysToKeep)
            {
                try
                {
                    _fileSystem.Directory.DeleteEntireFolder(dir);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to delete old log folder {Path}", dir.Path);
                }
            }
        }
    }
}