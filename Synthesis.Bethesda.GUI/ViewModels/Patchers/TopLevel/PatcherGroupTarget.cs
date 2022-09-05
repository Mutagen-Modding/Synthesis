using Mutagen.Bethesda.Plugins;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

public class PatcherGroupTarget : ViewModel, IModKeyProvider
{
    [Reactive] 
    public GroupVm? Group { get; set; } 

    public ModKey? ModKey => Group?.ModKey.Value;
}