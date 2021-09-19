using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IExecuteRun
    {
        Task Run(
            IGroupRun[] groups,
            CancellationToken cancel,
            DirectoryPath outputDir,
            FilePath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null);
    }

    public class ExecuteRun : IExecuteRun
    {
        private readonly ILogger _logger;
        private readonly IPrintRunStart _print;
        public IResetWorkingDirectory ResetWorkingDirectory { get; }
        public IRunAllGroups RunAllGroups { get; }
        public IEnsureSourcePathExists EnsureSourcePathExists { get; }

        public ExecuteRun(
            ILogger logger,
            IResetWorkingDirectory resetWorkingDirectory,
            IRunAllGroups runAllGroups,
            IPrintRunStart print,
            IEnsureSourcePathExists ensureSourcePathExists)
        {
            _logger = logger;
            _print = print;
            ResetWorkingDirectory = resetWorkingDirectory;
            RunAllGroups = runAllGroups;
            EnsureSourcePathExists = ensureSourcePathExists;
        }

        public async Task Run(
            IGroupRun[] groups,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            FilePath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null)
        {
            _print.Print(groups);
            
            cancellation.ThrowIfCancellationRequested();
            ResetWorkingDirectory.Reset();

            if (groups.Length == 0) return;

            cancellation.ThrowIfCancellationRequested();
            EnsureSourcePathExists.Ensure(sourcePath);

            cancellation.ThrowIfCancellationRequested();

            await RunAllGroups.Run(
                groups,
                cancellation,
                outputDir,
                sourcePath,
                persistenceMode,
                persistencePath);
        }
    }
}