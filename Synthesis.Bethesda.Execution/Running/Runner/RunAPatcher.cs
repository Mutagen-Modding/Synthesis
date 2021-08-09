using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunAPatcher
    {
        Task<FilePath?> Run(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            Task<Exception?> patcherPrep,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath);
    }

    public class RunAPatcher : IRunAPatcher
    {
        private readonly IFileSystem _fileSystem;
        private readonly IRunReporter _reporter;
        private readonly IFinalizePatcherRun _finalize;
        public IRunArgsConstructor GetRunArgs { get; }

        public RunAPatcher(
            IFileSystem fileSystem,
            IRunReporter reporter,
            IFinalizePatcherRun finalize,
            IRunArgsConstructor getRunArgs)
        {
            _fileSystem = fileSystem;
            _reporter = reporter;
            _finalize = finalize;
            GetRunArgs = getRunArgs;
        }

        public async Task<FilePath?> Run(
            ModKey outputKey,
            int key,
            IPatcherRun patcher,
            Task<Exception?> patcherPrep,
            CancellationToken cancellation,
            FilePath? sourcePath,
            string? persistencePath)
        {
            try
            {
                // Finish waiting for prep, if it didn't finish
                var prepException = await patcherPrep;
                if (prepException != null) return null;
                
                var args = GetRunArgs.GetArgs(
                    patcher,
                    key,
                    outputKey,
                    sourcePath,
                    persistencePath);
                
                _reporter.ReportStartingRun(key, patcher.Name);
                await patcher.Run(args,
                    cancel: cancellation);
                
                if (cancellation.IsCancellationRequested) return null;
            
                if (!_fileSystem.File.Exists(args.OutputPath))
                {
                    _reporter.ReportRunProblem(key, patcher.Name,
                        new ArgumentException($"Patcher {patcher.Name} did not produce output file."));
                    return null;
                }

                _reporter.ReportRunSuccessful(key, patcher.Name, args.OutputPath);
                return args.OutputPath;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _reporter.ReportRunProblem(key, patcher.Name, ex);
                return null;
            }
        }
    }
}