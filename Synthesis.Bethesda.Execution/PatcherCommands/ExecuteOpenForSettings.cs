using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Order;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.PatcherCommands;

public interface IExecuteOpenForSettings
{
    Task<int> Open(
        string path,
        bool directExe,
        ModKey modKey,
        IEnumerable<IModListingGetter> loadOrder,
        CancellationToken cancel);
}

public class ExecuteOpenForSettings : IExecuteOpenForSettings
{
    private readonly IFileSystem _fileSystem;
    private readonly IDefaultDataPathProvider _defaultDataPathProvider;
    private readonly IPatcherExtraDataPathProvider _patcherExtraDataPathProvider;
    private readonly IPatcherInternalDataPathProvider _internalDataPathProvider;
    
    public IGameReleaseContext GameReleaseContext { get; }
    public IDataDirectoryProvider DataDirectoryProvider { get; }
    public ISynthesisSubProcessRunner ProcessRunner { get; }
    public ITemporaryLoadOrderProvider LoadOrderProvider { get; }
    public IRunProcessStartInfoProvider RunProcessStartInfoProvider { get; }
    public IWindowPlacement WindowPlacement { get; }

    [ExcludeFromCodeCoverage]
    public ExecuteOpenForSettings(
        IGameReleaseContext gameReleaseContext,
        IDataDirectoryProvider dataDirectoryProvider,
        ITemporaryLoadOrderProvider loadOrderProvider,
        ISynthesisSubProcessRunner processRunner,
        IRunProcessStartInfoProvider runProcessStartInfoProvider,
        IWindowPlacement windowPlacement,
        IFileSystem fileSystem, 
        IDefaultDataPathProvider defaultDataPathProvider,
        IPatcherExtraDataPathProvider patcherExtraDataPathProvider,
        IPatcherInternalDataPathProvider internalDataPathProvider)
    {
        GameReleaseContext = gameReleaseContext;
        DataDirectoryProvider = dataDirectoryProvider;
        ProcessRunner = processRunner;
        LoadOrderProvider = loadOrderProvider;
        RunProcessStartInfoProvider = runProcessStartInfoProvider;
        WindowPlacement = windowPlacement;
        _fileSystem = fileSystem;
        _defaultDataPathProvider = defaultDataPathProvider;
        _patcherExtraDataPathProvider = patcherExtraDataPathProvider;
        _internalDataPathProvider = internalDataPathProvider;
    }
        
    public async Task<int> Open(
        string path,
        bool directExe,
        ModKey modKey,
        IEnumerable<IModListingGetter> loadOrder,
        CancellationToken cancel)
    {
        using var loadOrderFile = LoadOrderProvider.Get(loadOrder);

        var defaultDataFolderPath = _defaultDataPathProvider.Path;

        return await ProcessRunner.Run(
            RunProcessStartInfoProvider.GetStart(path, directExe, new OpenForSettings()
            {
                Left = (int)WindowPlacement.Left,
                Top = (int)WindowPlacement.Top,
                Height = (int)WindowPlacement.Height,
                Width = (int)WindowPlacement.Width,
                LoadOrderFilePath = loadOrderFile.File.Path,
                DataFolderPath = DataDirectoryProvider.Path,
                GameRelease = GameReleaseContext.Release,
                ExtraDataFolder = _patcherExtraDataPathProvider.Path,
                ModKey = modKey.FileName,
                DefaultDataFolderPath = _fileSystem.Directory.Exists(defaultDataFolderPath) ? defaultDataFolderPath.Path : null,
                InternalDataFolder = _fileSystem.Directory.Exists(_internalDataPathProvider.Path) ? _internalDataPathProvider.Path.Path : null,
            }),
            cancel: cancel).ConfigureAwait(false);
    }
}