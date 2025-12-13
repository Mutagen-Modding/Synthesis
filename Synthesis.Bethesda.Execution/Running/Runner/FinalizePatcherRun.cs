using System.IO.Abstractions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Analysis;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IFinalizePatcherRun
{
    FilePath? Finalize(
        IPatcherPrepAndRun patcher,
        FilePath outputPath);
}

public class FinalizePatcherRun : IFinalizePatcherRun
{
    private readonly IFileSystem _fileSystem;
    private readonly IRunReporter _reporter;

    public FinalizePatcherRun(
        IFileSystem fileSystem,
        IRunReporter reporter)
    {
        _fileSystem = fileSystem;
        _reporter = reporter;
    }

    public FilePath? Finalize(
        IPatcherPrepAndRun patcher,
        FilePath outputPath)
    {
        if (_fileSystem.File.Exists(outputPath))
        {
            _reporter.ReportRunSuccessful(patcher.Key, patcher.Name, outputPath);
            return outputPath;
        }

        // Check if split files exist (auto-split created multiple files)
        var outputModKey = ModKey.FromFileName(Path.GetFileName(outputPath));
        var outputModPath = new ModPath(outputModKey, outputPath);

        if (MultiModFileAnalysis.IsMultiModFile(outputModPath, fileSystem: _fileSystem))
        {
            // Split files exist - return the base path for the next patcher to detect
            _reporter.ReportRunSuccessful(patcher.Key, patcher.Name, outputPath);
            return outputPath;
        }

        return null;
    }
}