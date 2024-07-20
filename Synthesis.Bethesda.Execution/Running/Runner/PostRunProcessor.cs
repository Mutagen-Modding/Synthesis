using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary.Parameters;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public class PostRunProcessor
{
    private readonly IFileSystem _fileSystem;
    private readonly IGameReleaseContext _gameReleaseContext;
    private readonly IDataDirectoryProvider _dataDirectoryProvider;
    private readonly ILoadOrderForRunProvider _loadOrderForRunProvider;
    private readonly IProfileDirectories _profileDirectories;

    public PostRunProcessor(
        IFileSystem fileSystem,
        IGameReleaseContext gameReleaseContext,
        IDataDirectoryProvider dataDirectoryProvider,
        ILoadOrderForRunProvider loadOrderForRunProvider,
        IProfileDirectories profileDirectories)
    {
        _fileSystem = fileSystem;
        _gameReleaseContext = gameReleaseContext;
        _dataDirectoryProvider = dataDirectoryProvider;
        _loadOrderForRunProvider = loadOrderForRunProvider;
        _profileDirectories = profileDirectories;
    }
    
    public async Task<FilePath> Run(
        IGroupRun groupRun,
        ModPath path,
        IReadOnlySet<ModKey> blackListMod)
    {
        var lo = new LoadOrder<IModFlagsGetter>(
            _loadOrderForRunProvider
                .Get(path.ModKey, blackListMod)
                .Select(listing =>
                {
                    var modPath = new ModPath(listing.ModKey,
                        _dataDirectoryProvider.Path.GetFile(listing.ModKey.FileName).Path);
                    if (!_fileSystem.File.Exists(modPath)) return null;
                    using var mod = ModInstantiator.ImportGetter(modPath, _gameReleaseContext.Release,
                        new BinaryReadParameters()
                        {
                            FileSystem = _fileSystem
                        });
                    return new ModFlags(mod);
                })
                .NotNull());
        
        var mod = ModInstantiator.ImportGetter(
            path,
            _gameReleaseContext.Release,
            new BinaryReadParameters()
            {
                LoadOrder = lo
            });

        var postProcessPath = new FilePath(
            Path.Combine(_profileDirectories.WorkingDirectory, groupRun.ModKey.Name, $"PostProcess", groupRun.ModKey.FileName));

        postProcessPath.Directory?.Create(_fileSystem);
        
        mod.WriteToBinary(postProcessPath, new BinaryWriteParameters()
        {
            FileSystem = _fileSystem,
            LoadOrder = lo
        });
        
        return postProcessPath;
    }
}