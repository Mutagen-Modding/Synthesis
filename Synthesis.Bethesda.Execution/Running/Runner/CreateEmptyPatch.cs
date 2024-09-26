using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface ICreateEmptyPatch
{
    FilePath Create(ModKey modKey, RunParameters runParameters);
}

public class CreateEmptyPatch : ICreateEmptyPatch
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IGameReleaseContext _gameReleaseContext;
    public IProfileDirectories ProfileDirectories { get; }
    
    public CreateEmptyPatch(
        ILogger logger,
        IFileSystem fileSystem,
        IProfileDirectories profileDirectories,
        IGameReleaseContext gameReleaseContext)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _gameReleaseContext = gameReleaseContext;
        ProfileDirectories = profileDirectories;
    }

    public FilePath Create(ModKey modKey, RunParameters runParameters)
    {
        var path = new FilePath(Path.Combine(ProfileDirectories.SeedDirectory, modKey.FileName));
        _logger.Information("Creating seed mod at {Path}", path);
        path.Directory?.Create(_fileSystem);
        var mod = ModInstantiator.Activator(modKey, _gameReleaseContext.Release,
            headerVersion: runParameters.HeaderVersionOverride,
            forceUseLowerFormIDRanges: runParameters.FormIDRangeMode.ToForceBool());
        mod.IsMaster = runParameters.Master;
        mod.BeginWrite
            .ToPath(path)
            .WithNoLoadOrder()
            .WithFileSystem(_fileSystem)
            .WithForcedLowerFormIdRangeUsage(runParameters.FormIDRangeMode.ToForceBool())
            .NoNextFormIDProcessing()
            .Write();
        return path;
    }
}