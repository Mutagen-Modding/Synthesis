using Autofac;
using Noggog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public interface IRunFactory
{
    RunVm GetRun(IEnumerable<GroupVm> groups);
}

public class RunFactory : IRunFactory
{
    private readonly ILifetimeScope _scope;

    public RunFactory(
        ILifetimeScope scope)
    {
        _scope = scope;
    }
        
    public RunVm GetRun(IEnumerable<GroupVm> groups)
    {
        var runScope = _scope.BeginLifetimeScope(LifetimeScopes.RunNickname);
        var factory = runScope.Resolve<RunVm.Factory>();
        var ret = factory(groups);
        runScope.DisposeWith(ret);
        return ret;
    }
}