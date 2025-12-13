using Autofac;
using Noggog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

public interface IPatcherRunnerFactory
{
    PatcherRunVm ToRunner(PatcherVm patcherVm);
}

public class PatcherRunnerFactory : IPatcherRunnerFactory
{
    public PatcherRunVm ToRunner(PatcherVm patcherVm)
    {
        switch (patcherVm)
        {
            case GitPatcherVm:
            case SolutionPatcherVm:
                return ToRunner<SolutionPatcherPrepAndRun>(patcherVm);
            case CliPatcherVm:
                return ToRunner<CliPatcherRun>(patcherVm);
            default:
                throw new NotImplementedException();
        }
    }
        
    private PatcherRunVm ToRunner<T>(PatcherVm patcherVm)
        where T :  IPatcherPrepAndRun
    {
        var scope = patcherVm.Scope.BeginLifetimeScope(LifetimeScopes.RunNickname, c =>
        {
            c.RegisterType<T>().As<IPatcherPrepAndRun>();
            c.RegisterType<ReporterLoggerWrapper>()
                .AsImplementedInterfaces()
                .SingleInstance();
        });
        var runnerFactory = scope.Resolve<PatcherRunVm.Factory>();
        patcherVm.PrepForRun();
        var ret = runnerFactory(patcherVm);
        scope.DisposeWith(ret);
        return ret;
    }
}