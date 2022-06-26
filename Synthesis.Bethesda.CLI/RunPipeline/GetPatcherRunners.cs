using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public interface IGetPatcherRunners
{
    IPatcherRun[] Get(IEnumerable<PatcherSettings> patcherSettings);
}

public class GetPatcherRunners : IGetPatcherRunners
{
    private readonly ILogger _logger;
    public IPatcherSettingsToRunnerFactory PatcherSettingsToRunnerFactory { get; }

    public GetPatcherRunners(
        ILogger logger,
        IPatcherSettingsToRunnerFactory patcherSettingsToRunnerFactory)
    {
        _logger = logger;
        PatcherSettingsToRunnerFactory = patcherSettingsToRunnerFactory;
    }
        
    public IPatcherRun[] Get(IEnumerable<PatcherSettings> patcherSettings)
    {
        return patcherSettings
            .Where(p => p.On)
            .Select(patcherSettings =>
            {
                patcherSettings.Print(_logger);
                    
                return PatcherSettingsToRunnerFactory.Convert(patcherSettings);
            })
            .ToArray();
    }
}