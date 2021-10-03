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
        private readonly IProfileGroupsList _groupsList;
        private readonly Func<GroupVm> _groupCreator;

        public NewGroupCreator(
            IProfileGroupsList groupsList,
            Func<GroupVm> groupCreator)
        {
            _groupsList = groupsList;
            _groupCreator = groupCreator;
        }
        
        public GroupVm Get()
        {
            var ret = _groupCreator();
            ret.Expanded = true;
            ret.IsOn = true;
            if (_groupsList.Groups.Count == 0)
            {
                ret.Name = Constants.SynthesisName;
            }
            return ret;
        }
    }
}