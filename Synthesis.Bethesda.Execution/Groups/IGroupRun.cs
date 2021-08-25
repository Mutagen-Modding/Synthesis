using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Running.Cli;
using Synthesis.Bethesda.Execution.Running.Runner;

namespace Synthesis.Bethesda.Execution.Groups
{
    public interface IGroupRun
    {
        ModKey ModKey { get; }
        PatcherPrepBundle[] Patchers { get; }
    }

    public class GroupRun : IGroupRun
    {
        public ModKey ModKey { get; }
        public PatcherPrepBundle[] Patchers { get; }

        public GroupRun(ModKey modKey, PatcherPrepBundle[] patchers)
        {
            Patchers = patchers;
            ModKey = modKey;
        }
    }
}