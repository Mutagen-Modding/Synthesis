using Mutagen.Bethesda.Plugins;

namespace Synthesis.Bethesda.GUI.ViewModels.Groups;

public interface IModKeyProvider
{
    ModKey? ModKey { get; }
}