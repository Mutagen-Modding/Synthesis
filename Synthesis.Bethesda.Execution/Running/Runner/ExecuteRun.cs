using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
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
        public IResetWorkingDirectory ResetWorkingDirectory { get; }
        public IRunAllGroups RunAllGroups { get; }
        public IEnsureSourcePathExists EnsureSourcePathExists { get; }

        public ExecuteRun(
            IResetWorkingDirectory resetWorkingDirectory,
            IRunAllGroups runAllGroups,
            IEnsureSourcePathExists ensureSourcePathExists)
        {
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