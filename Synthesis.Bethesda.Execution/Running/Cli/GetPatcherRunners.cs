using System;
using System.Linq;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Cli
{
    public interface IGetPatcherRunners
    {
        IPatcherRun[] Get();
    }

    public class GetPatcherRunners : IGetPatcherRunners
    {
        private readonly ILogger _logger;
        public IPatcherSettingsToRunnerFactory PatcherSettingsToRunnerFactory { get; }
        public ISynthesisProfileSettings Profile { get; }

        public GetPatcherRunners(
            ILogger logger,
            IPatcherSettingsToRunnerFactory patcherSettingsToRunnerFactory,
            ISynthesisProfileSettings profile)
        {
            _logger = logger;
            PatcherSettingsToRunnerFactory = patcherSettingsToRunnerFactory;
            Profile = profile;
        }
        
        public IPatcherRun[] Get()
        {
            _logger.Information("Patchers to run:");
            return Profile.Patchers
                .Where(p => p.On)
                .Select(patcherSettings =>
                {
                    patcherSettings.Print(_logger);
                    
                    return PatcherSettingsToRunnerFactory.Convert(patcherSettings);
                })
                .ToArray();
        }
    }
}