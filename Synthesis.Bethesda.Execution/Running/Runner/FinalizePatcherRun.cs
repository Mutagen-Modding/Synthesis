using System;
using System.IO.Abstractions;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IFinalizePatcherRun
    {
        bool Finalize(
            IPatcherRun patcher,
            int key,
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
        
        public bool Finalize(
            IPatcherRun patcher,
            int key,
            FilePath outputPath)
        {
            if (!_fileSystem.File.Exists(outputPath))
            {
                _reporter.ReportRunProblem(key, patcher.Name,
                    new ArgumentException($"Patcher {patcher.Name} did not produce output file."));
                return false;
            }

            _reporter.ReportRunSuccessful(key, patcher.Name, outputPath);
            return true;
        }
    }
}