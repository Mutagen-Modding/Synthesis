using Mutagen.Bethesda.Plugins;
using Serilog;
using Synthesis.Bethesda.CLI.Services.Common;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public interface IGetGroupRunners
{
    IGroupRun[] Get(CancellationToken cancellation);
}

public class GetGroupRunners : IGetGroupRunners
{
    private readonly ILogger _logger;
    private readonly IGetPatcherRunners _getPatcherRunners;
    private readonly IPrepPatcherForRun _prepPatcherForRun;
    private readonly IProfileProvider _profile;

    public GetGroupRunners(
        ILogger logger,
        IGetPatcherRunners getPatcherRunners,
        IPrepPatcherForRun prepPatcherForRun,
        IProfileProvider profile)
    {
        _logger = logger;
        _getPatcherRunners = getPatcherRunners;
        _prepPatcherForRun = prepPatcherForRun;
        _profile = profile;
    }
        
    public IGroupRun[] Get(CancellationToken cancellation)
    {
        _logger.Information("Groups to run:");
        return _profile.Profile.Value.Groups
            .Where(x => x.On)
            .Select<PatcherGroupSettings, IGroupRun>(settings =>
            {
                return new GroupRun(
                    ModKey.FromName(settings.Name, _profile.Profile.Value.ExportAsMasterFiles ? ModType.Master : ModType.Plugin),
                    _getPatcherRunners.Get(settings.Patchers).Select(patcher =>
                    {
                        return _prepPatcherForRun.Prep(patcher, cancellation);
                    }).ToArray(),
                    settings.BlacklistedMods.ToHashSet());
            })
            .ToArray();
    }
}