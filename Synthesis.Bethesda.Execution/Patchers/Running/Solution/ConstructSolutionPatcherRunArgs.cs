using System.IO.Abstractions;
using Mutagen.Bethesda.Strings;
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
    public IPatcherExtraDataPathProvider PatcherExtraDataPathProvider { get; }
    public IDefaultDataPathProvider DefaultDataPathProvider { get; }

    public ConstructSolutionPatcherRunArgs(
        IFileSystem fileSystem,
        IPatcherInternalDataPathProvider internalDataPathProvider,
        IPatcherExtraDataPathProvider patcherExtraDataPathProvider,
        IDefaultDataPathProvider defaultDataPathProvider)
    {
        _fileSystem = fileSystem;
        _internalDataPathProvider = internalDataPathProvider;
        PatcherExtraDataPathProvider = patcherExtraDataPathProvider;
        DefaultDataPathProvider = defaultDataPathProvider;
    }
        
    public RunSynthesisMutagenPatcher Construct(RunSynthesisPatcher settings)
    {
        var defaultDataFolderPath = DefaultDataPathProvider.Path;

        return new RunSynthesisMutagenPatcher()
        {
            DataFolderPath = settings.DataFolderPath,
            ExtraDataFolder = PatcherExtraDataPathProvider.Path,
            DefaultDataFolderPath = _fileSystem.Directory.Exists(defaultDataFolderPath) ? defaultDataFolderPath.Path : null,
            GameRelease = settings.GameRelease,
            LoadOrderFilePath = settings.LoadOrderFilePath,
            OutputPath = settings.OutputPath,
            SourcePath = settings.SourcePath,
            InternalDataFolder = _fileSystem.Directory.Exists(_internalDataPathProvider.Path) ? _internalDataPathProvider.Path.Path : null,
            PatcherName = settings.PatcherName,
            PersistencePath = settings.PersistencePath,
            TargetLanguage = Enum.Parse<Language>(settings.TargetLanguage),
            Localize = settings.Localize,
            ModKey = settings.ModKey,
            UseUtf8ForEmbeddedStrings = settings.UseUtf8ForEmbeddedStrings,
            HeaderVersionOverride = settings.HeaderVersionOverride,
            FormIDRangeMode = settings.FormIDRangeMode,
        };
    }
}