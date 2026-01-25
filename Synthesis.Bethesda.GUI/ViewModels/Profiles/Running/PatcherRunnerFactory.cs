using Autofac;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Reporters;
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
                return ToRunner<GitPatcherRun>(patcherVm);
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
        // Capture the inner logger from the parent scope before creating the child scope.
        // This prevents circular dependency when ReporterLoggerWrapper needs an ILogger.
        var innerLogger = patcherVm.Scope.Resolve<ILogger>();

        var scope = patcherVm.Scope.BeginLifetimeScope(LifetimeScopes.RunNickname, c =>
        {
            c.RegisterType<T>().As<IPatcherPrepAndRun>();

            // Register ReporterLoggerWrapper using a factory to manually construct
            // it with the captured innerLogger to prevent circular dependency.
            c.Register(ctx => new ReporterLoggerWrapper(
                    ctx.Resolve<IProfileNameProvider>(),
                    ctx.Resolve<IPatcherNameProvider>(),
                    ctx.Resolve<IPatcherIdProvider>(),
                    ctx.Resolve<IRunReporter>(),
                    innerLogger))
                .AsImplementedInterfaces()
                .SingleInstance();

            // Re-register the specific runner types in this scope so they get
            // ReporterLoggerWrapper as their ILogger instead of the root logger.
            // Only re-register the runners, not entire modules, to avoid conflicts.
            if (typeof(T) == typeof(GitPatcherRun))
            {
                c.RegisterType<GitPatcherRunner>().AsImplementedInterfaces();
            }
            else if (typeof(T) == typeof(SolutionPatcherPrepAndRun))
            {
                c.RegisterType<SolutionPatcherRunner>().AsImplementedInterfaces();
            }
        });
        var runnerFactory = scope.Resolve<PatcherRunVm.Factory>();
        patcherVm.PrepForRun();
        var ret = runnerFactory(patcherVm);
        scope.DisposeWith(ret);
        return ret;
    }
}