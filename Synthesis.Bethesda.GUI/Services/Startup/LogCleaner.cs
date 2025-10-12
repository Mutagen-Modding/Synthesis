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
        if (!_fileSystem.Directory.Exists(LogSettings.LogFolder)) return;

        foreach (var file in _fileSystem.Directory.EnumerateFilePaths(LogSettings.LogFolder, recursive: false))
        {
            // Skip Current.log
            if (string.Equals(file.Name, "Current.log", StringComparison.OrdinalIgnoreCase)) continue;

            // Try to parse the date from the log file name format: MM-dd-yyyy_HHhMMmSSs.log
            var fileName = file.NameWithoutExtension;
            if (fileName.Length < 10) continue;

            var datePart = fileName.Substring(0, 10); // MM-dd-yyyy
            if (!DateTime.TryParseExact(datePart, "MM-dd-yyyy", default, default, out var dt)) continue;

            if ((NowProvider.NowLocal - dt).TotalDays > DaysToKeep)
            {
                try
                {
                    _fileSystem.File.Delete(file);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Failed to delete old log file {Path}", file.Path);
                }
            }
        }
    }
}