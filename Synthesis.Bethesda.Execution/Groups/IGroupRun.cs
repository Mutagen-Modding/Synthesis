using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Running.Runner;

namespace Synthesis.Bethesda.Execution.Groups
{
    public interface IGroupRun
    {
        ModKey ModKey { get; }
        PatcherPrepBundle[] Patchers { get; }
        IReadOnlySet<ModKey> BlacklistedMods { get; }
    }

    public class GroupRun : IGroupRun
    {
        public ModKey ModKey { get; }
        public PatcherPrepBundle[] Patchers { get; }
        public IReadOnlySet<ModKey> BlacklistedMods { get; }

        public GroupRun(ModKey modKey, PatcherPrepBundle[] patchers, IReadOnlySet<ModKey> blackListedMods)
        {
            Patchers = patchers;
            ModKey = modKey;
            BlacklistedMods = blackListedMods;
        }
    }
}