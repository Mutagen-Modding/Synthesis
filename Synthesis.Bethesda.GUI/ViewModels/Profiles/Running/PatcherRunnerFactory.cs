using System;
using Autofac;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running
{
    public interface IPatcherRunnerFactory
    {
        PatcherRunVm ToRunner(PatchersRunVm parent, PatcherVm patcherVm);
    }

    public class PatcherRunnerFactory : IPatcherRunnerFactory
    {
        public PatcherRunVm ToRunner(PatchersRunVm parent, PatcherVm patcherVm)
        {
            switch (patcherVm)
            {
                case GitPatcherVm:
                case SolutionPatcherVm:
                    return ToRunner<SolutionPatcherRun>(parent, patcherVm);
                case CliPatcherVm:
                    return ToRunner<CliPatcherRun>(parent, patcherVm);
                default:
                    throw new NotImplementedException();
            }
        }
        
        private PatcherRunVm ToRunner<T>(PatchersRunVm parent, PatcherVm patcherVm)
            where T :  IPatcherRun
        {
            var scope = patcherVm.Scope.BeginLifetimeScope(c =>
            {
                c.RegisterType<T>().As<IPatcherRun>();
                c.RegisterInstance(parent.Reporter).As<IRunReporter<int>>();
                c.RegisterType<ReporterLoggerWrapper>()
                    .AsImplementedInterfaces()
                    .SingleInstance();
            });
            var runnerFactory = scope.Resolve<PatcherRunVm.Factory>();
            var ret = runnerFactory(parent, patcherVm);
            scope.DisposeWith(ret);
            return ret;
        }
    }
}