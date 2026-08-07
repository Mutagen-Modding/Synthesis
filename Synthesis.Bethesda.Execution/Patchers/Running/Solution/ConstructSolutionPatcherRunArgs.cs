using System.IO.Abstractions;
using Mutagen.Bethesda.Synthesis.CLI;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution;

public interface IConstructSolutionPatcherRunArgs
{
    RunSynthesisMutagenPatcher Construct(RunSynthesisPatcher settings);
}

public class ConstructSolutionPatcherRunArgs : IConstructSolutionPatcherRunArgs
{
    private readonly IFileSystem _fileSystem;
    private readonly IPatcherInternalDataPathProvider _internalDataPathProvider;
    public IConstructBaseRunArgs BaseRunArgs { get; }
    public IDefaultDataPathProvider DefaultDataPathProvider { get; }

    public ConstructSolutionPatcherRunArgs(
        IFileSystem fileSystem,
        IConstructBaseRunArgs baseRunArgs,
        IPatcherInternalDataPathProvider internalDataPathProvider,
        IDefaultDataPathProvider defaultDataPathProvider)
    {
        _fileSystem = fileSystem;
        BaseRunArgs = baseRunArgs;
        _internalDataPathProvider = internalDataPathProvider;
        DefaultDataPathProvider = defaultDataPathProvider;
    }

    public RunSynthesisMutagenPatcher Construct(RunSynthesisPatcher settings)
    {
        var defaultDataFolderPath = DefaultDataPathProvider.Path;

        var ret = BaseRunArgs.Construct(settings);
        ret.DefaultDataFolderPath = _fileSystem.Directory.Exists(defaultDataFolderPath) ? defaultDataFolderPath.Path : null;
        ret.InternalDataFolder = _fileSystem.Directory.Exists(_internalDataPathProvider.Path) ? _internalDataPathProvider.Path.Path : null;
        return ret;
    }
}
