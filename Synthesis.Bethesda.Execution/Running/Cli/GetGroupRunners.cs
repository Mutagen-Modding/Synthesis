using System.Linq;
using System.Threading;
using Serilog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.Execution.Running.Cli
{
    public interface IGetGroupRunners
    {
        IGroupRun[] Get(CancellationToken cancellation);
    }

    public class GetGroupRunners : IGetGroupRunners
    {
        private readonly ILogger _logger;
        private readonly IGetPatcherRunners _getPatcherRunners;
        private readonly IPrepPatcherForRun _prepPatcherForRun;
        private readonly ISynthesisProfileSettings _profile;

        public GetGroupRunners(
            ILogger logger,
            IGetPatcherRunners getPatcherRunners,
            IPrepPatcherForRun prepPatcherForRun,
            ISynthesisProfileSettings profile)
        {
            _logger = logger;
            _getPatcherRunners = getPatcherRunners;
            _prepPatcherForRun = prepPatcherForRun;
            _profile = profile;
        }
        
        public IGroupRun[] Get(CancellationToken cancellation)
        {
            _logger.Information("Groups to run:");
            return _profile.Groups
                .Where(x => x.On)
                .Select<PatcherGroupSettings, IGroupRun>(settings =>
                {
                    return new GroupRun(
                        settings.Name,
                        _getPatcherRunners.Get(settings.Patchers).Select(patcher =>
                        {
                            return _prepPatcherForRun.Prep(patcher, cancellation);
                        }).ToArray(),
                        settings.BlacklistedMods.ToHashSet());
                })
                .ToArray();
        }
    }
}