using System;
using Autofac;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.TopLevel
{
    public interface IPatcherRunnerFactory
    {
        ISolutionPatcherRun Sln();
    }

    public class PatcherRunnerFactory : IPatcherRunnerFactory
    {
        private readonly ILifetimeScope _scope;

        public PatcherRunnerFactory(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public ISolutionPatcherRun Sln()
        {
            var scope = _scope.BeginLifetimeScope();
            var ret = scope.Resolve<SolutionPatcherRun>();
            ret.AddForDisposal(scope);
            return ret;
        }
    }
}