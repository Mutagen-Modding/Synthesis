﻿using System.IO.Abstractions;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IFinalizePatcherRun
{
    FilePath? Finalize(
        IPatcherRun patcher,
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
        IPatcherRun patcher,
        FilePath outputPath)
    {
        if (!_fileSystem.File.Exists(outputPath))
        {
            return null;
        }

        _reporter.ReportRunSuccessful(patcher.Key, patcher.Name, outputPath);
        return outputPath;
    }
}