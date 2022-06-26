using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Running.Runner;

namespace Synthesis.Bethesda.Execution.Modules;

public class ProfileModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IExecuteRun).Assembly)
            .InNamespacesOf(
                typeof(IProfileDirectories))
            .AsMatchingInterface();
        builder.RegisterAssemblyTypes(typeof(IExecuteRun).Assembly)
            .InNamespacesOf(
                typeof(IGroupLoadOrderProvider))
            .AsMatchingInterface();
        builder.RegisterAssemblyTypes(typeof(IExecuteRun).Assembly)
            .InNamespacesOf(
                typeof(IExecuteRun))
            .AsMatchingInterface();
    }
}