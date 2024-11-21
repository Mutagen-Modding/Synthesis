using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Running.Cli;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;

namespace Synthesis.Bethesda.Execution.Modules;

public class PatcherModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IPatcherNameProvider).Assembly)
            .InNamespacesOf(
                typeof(IPatcherIdProvider),
                typeof(ICliPatcherRun),
                typeof(ICliNameConverter),
                typeof(IPatcherNameProvider),
                typeof(IPrintShaIfApplicable))
            .NotInjection()
            .AsImplementedInterfaces()
            .AsSelf();
    }
}