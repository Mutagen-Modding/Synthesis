using System.Collections.Generic;
using System.Linq;
using Synthesis.Bethesda.Execution.Settings.V2;
using Vers1 = Synthesis.Bethesda.Execution.Settings.V1.PipelineSettings;
using Vers2 = Synthesis.Bethesda.Execution.Settings.V2.PipelineSettings;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V1
{
    public interface IPipelineSettingsV1Upgrader
    {
        Vers2 Upgrade(Vers1 input);
    }

    public class PipelineSettingsV1Upgrader : IPipelineSettingsV1Upgrader
    {
        public Vers2 Upgrade(Vers1 input)
        {
            return new Vers2()
            {
                Profiles = input.Profiles.Select(x => (ISynthesisProfileSettings)new SynthesisProfile()
                {
                    Nickname = x.Nickname,
                    ID = x.ID,
                    TargetRelease = x.TargetRelease,
                    MutagenVersioning = x.MutagenVersioning,
                    MutagenManualVersion = x.MutagenManualVersion,
                    SynthesisVersioning = x.SynthesisVersioning,
                    SynthesisManualVersion = x.SynthesisManualVersion,
                    DataPathOverride = x.DataPathOverride,
                    ConsiderPrereleaseNugets = x.ConsiderPrereleaseNugets,
                    LockToCurrentVersioning = x.LockToCurrentVersioning,
                    Persistence = x.Persistence,
                    IgnoreMissingMods = x.IgnoreMissingMods,
                    Groups = new List<PatcherGroupSettings>()
                    {
                        new PatcherGroupSettings()
                        {
                            Name = "Main Group",
                            ModKey = Synthesis.Bethesda.Constants.SynthesisName,
                            On = true,
                            Patchers = x.Patchers
                        }
                    },
                }).ToList()
            };
        }
    }
}