using Mutagen.Bethesda.Plugins;

namespace Synthesis.Bethesda.Execution.Settings.V2;

public class PatcherGroupSettings
{
    public bool On { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<PatcherSettings> Patchers { get; set; } = new();
    public bool Expanded { get; set; }
    public List<ModKey> BlacklistedMods { get; set; } = new();
}