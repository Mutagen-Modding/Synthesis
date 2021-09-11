using System;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation
{
    public interface INewGroupCreator
    {
        GroupVm Get();
    }

    public class NewGroupCreator : INewGroupCreator
    {
        private readonly Func<GroupVm> _groupCreator;

        public NewGroupCreator(
            Func<GroupVm> groupCreator)
        {
            _groupCreator = groupCreator;
        }
        
        public GroupVm Get()
        {
            var ret = _groupCreator();
            ret.Expanded = true;
            ret.IsOn = true;
            return ret;
        }
    }
}