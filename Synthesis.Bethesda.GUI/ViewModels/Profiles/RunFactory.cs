using Autofac;
using Noggog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IRunFactory
    {
        PatchersRunVm GetRun();
    }

    public class RunFactory : IRunFactory
    {
        private readonly ILifetimeScope _scope;

        public RunFactory(
            ILifetimeScope scope)
        {
            _scope = scope;
        }
        
        public PatchersRunVm GetRun()
        {
            var runScope = _scope.BeginLifetimeScope(LifetimeScopes.RunNickname);
            var runsVm = runScope.Resolve<PatchersRunVm>();
            runScope.DisposeWith(runsVm);
            return runsVm;
        }
    }
}