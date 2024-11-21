using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Headers;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Utility.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public class PostRunProcessor
{
    private readonly IFileSystem _fileSystem;
    private readonly IGameReleaseContext _gameReleaseContext;
    private readonly IDataDirectoryProvider _dataDirectoryProvider;
    private readonly ILoadOrderForRunProvider _loadOrderForRunProvider;
    private readonly IProfileDirectories _profileDirectories;
    private readonly IModCompactor _modCompactor;

    public PostRunProcessor(
        IFileSystem fileSystem,
        IGameReleaseContext gameReleaseContext,
        IDataDirectoryProvider dataDirectoryProvider,
        ILoadOrderForRunProvider loadOrderForRunProvider,
        IProfileDirectories profileDirectories,
        IModCompactor modCompactor)
    {
        _fileSystem = fileSystem;
        _gameReleaseContext = gameReleaseContext;
        _dataDirectoryProvider = dataDirectoryProvider;
        _loadOrderForRunProvider = loadOrderForRunProvider;
        _profileDirectories = profileDirectories;
        _modCompactor = modCompactor;
    }
    
    public async Task<FilePath> Run(
        IGroupRun groupRun,
        ModPath path,
        IReadOnlySet<ModKey> blackListMod,
        RunParameters runParameters)
    {
        var lo = new LoadOrder<IModMasterStyledGetter>(
            _loadOrderForRunProvider
                .Get(path.ModKey, blackListMod)
                .Select(listing =>
                {
                    var modPath = new ModPath(listing.ModKey,
                        _dataDirectoryProvider.Path.GetFile(listing.ModKey.FileName).Path);
                    if (!_fileSystem.File.Exists(modPath)) return null;
                    var modHeader = ModHeaderFrame.FromPath(modPath, _gameReleaseContext.Release);
                    return new KeyedMasterStyle(modPath.ModKey, modHeader.MasterStyle);
                })
                .WhereNotNull());
        
        var mod = ModInstantiator.ImportSetter(
            path,
            _gameReleaseContext.Release,
            new BinaryReadParameters()
            {
                MasterFlagsLookup = lo,
                FileSystem = _fileSystem
            });

        var postProcessPath = new FilePath(
            Path.Combine(_profileDirectories.WorkingDirectory, groupRun.ModKey.Name, $"PostProcess", groupRun.ModKey.FileName));

        postProcessPath.Directory?.Create(_fileSystem);

        if (runParameters.MasterStyle != MasterStyle.Full)
        {
            if (runParameters.MasterStyleFallbackEnabled)
            {
                _modCompactor.CompactToWithFallback(mod, runParameters.MasterStyle);
            }
            else
            {
                _modCompactor.CompactTo(mod, runParameters.MasterStyle);
            }
        }
        
        await mod.BeginWrite
            .ToPath(postProcessPath)
            .WithLoadOrder(lo)
            .WithFileSystem(_fileSystem)
            .WriteAsync();
        
        return postProcessPath;
    }
}