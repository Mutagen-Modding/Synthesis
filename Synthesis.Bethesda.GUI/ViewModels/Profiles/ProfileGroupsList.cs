using DynamicData;
using Noggog.WPF;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IProfileGroupsList
    {
        SourceList<GroupVm> Groups { get; }
    }

    public class ProfileGroupsList : ViewModel, IProfileGroupsList
    {
        public SourceList<GroupVm> Groups { get; } = new();
    }
}