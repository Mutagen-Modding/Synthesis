using Noggog;
using Noggog.WPF;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

public interface IRunItem : ISelectable
{
    ViewModel SourceVm { get; }
    bool AutoScrolling { get; set; }
}