using DynamicData;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.ViewModels.Groups;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

public interface IGroupFactory
{
    GroupVm Get(PatcherGroupSettings groupSettings);
}

public class GroupFactory : IGroupFactory
{
    private readonly Func<GroupVm> _groupCreator;
    private readonly IPatcherFactory _patcherFactory;

    public GroupFactory(
        Func<GroupVm> groupCreator,
        IPatcherFactory patcherFactory)
    {
        _groupCreator = groupCreator;
        _patcherFactory = patcherFactory;
    }
        
    public GroupVm Get(PatcherGroupSettings groupSettings)
    {
        var group = _groupCreator();
        group.Name = groupSettings.Name;
        group.IsOn = groupSettings.On;
        group.BlacklistedModKeys.AddRange(groupSettings.BlacklistedMods);
        group.Expanded = groupSettings.Expanded;
        group.Patchers.AddRange(
            groupSettings.Patchers
                .Select(x =>
                {
                    var ret = _patcherFactory.Get(x);
                    ret.GroupTarget.Group = group;
                    return ret;
                }));
        return group;
    }
}