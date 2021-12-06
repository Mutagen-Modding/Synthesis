using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Services.Profile.Running
{
    public interface IExecuteGuiRun
    {
        Task Run(
            IEnumerable<IGroupRun> groupRuns,
            PersistenceMode persistenceMode,
            CancellationToken cancel);
    }

    public class ExecuteGuiRun : IExecuteGuiRun
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IExecuteRun _executeRun;
        private readonly IDataDirectoryProvider _dataDirectoryProvider;
        private readonly IProfileDirectories _profileDirectories;

        public ExecuteGuiRun(
            ILogger logger,
            IFileSystem fileSystem,
            IExecuteRun executeRun,
            IDataDirectoryProvider dataDirectoryProvider,
            IProfileDirectories profileDirectories)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _executeRun = executeRun;
            _dataDirectoryProvider = dataDirectoryProvider;
            _profileDirectories = profileDirectories;
        }
        
        public async Task Run(
            IEnumerable<IGroupRun> groupRuns,
            PersistenceMode persistenceMode,
            CancellationToken cancel)
        {
            var outputDir = new DirectoryPath(Path.Combine(_profileDirectories.WorkingDirectory, "Output"));
            _fileSystem.Directory.DeleteEntireFolder(outputDir.Path);
            await _executeRun.Run(
                groups: groupRuns.ToArray(),
                cancel: cancel,
                outputDir: outputDir,
                persistenceMode: persistenceMode,
                persistencePath: Path.Combine(_profileDirectories.ProfileDirectory, "Persistence")).ConfigureAwait(false);
        }
    }
}