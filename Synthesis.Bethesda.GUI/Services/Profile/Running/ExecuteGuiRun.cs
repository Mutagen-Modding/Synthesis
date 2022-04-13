using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Strings;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.GUI.Services.Profile.Running;

public interface IExecuteGuiRun
{
    Task Run(
        IEnumerable<IGroupRun> groupRuns,
        PersistenceMode persistenceMode,
        bool localize,
        Language targetLanguage,
        CancellationToken cancel);
}

public class ExecuteGuiRun : IExecuteGuiRun
{
    private readonly IFileSystem _fileSystem;
    private readonly IExecuteRun _executeRun;
    private readonly IDataDirectoryProvider _dataDirectoryProvider;
    private readonly IProfileDirectories _profileDirectories;

    public ExecuteGuiRun(
        IFileSystem fileSystem,
        IExecuteRun executeRun,
        IDataDirectoryProvider dataDirectoryProvider,
        IProfileDirectories profileDirectories)
    {
        _fileSystem = fileSystem;
        _executeRun = executeRun;
        _dataDirectoryProvider = dataDirectoryProvider;
        _profileDirectories = profileDirectories;
    }
        
    public async Task Run(
        IEnumerable<IGroupRun> groupRuns,
        PersistenceMode persistenceMode,
        bool localize,
        Language targetLanguage,
        CancellationToken cancel)
    {
        var outputDir = _dataDirectoryProvider.Path;
        await _executeRun.Run(
            groups: groupRuns.ToArray(),
            cancel: cancel,
            outputDir: outputDir,
            runParameters: new RunParameters(
                targetLanguage,
                Localize: localize,
                persistenceMode,
                Path.Combine(_profileDirectories.ProfileDirectory, "Persistence"))).ConfigureAwait(false);
    }
}