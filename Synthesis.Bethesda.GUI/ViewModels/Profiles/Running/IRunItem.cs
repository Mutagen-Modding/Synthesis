using Noggog.WPF;
using Noggog.WPF.Interfaces;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public interface IRunItem : ISelectable
    {
        ViewModel SourceVm { get; }
        bool AutoScrolling { get; set; }
    }
}