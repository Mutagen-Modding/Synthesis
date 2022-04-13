using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Modules;

public class SolutionPatcherModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<PatcherModule>();
            
        builder.RegisterAssemblyTypes(typeof(IPathToProjProvider).Assembly)
            .InNamespacesOf(
                typeof(IPathToProjProvider),
                typeof(IGithubPatcherIdentifier))
            .NotInjection()
            .AsImplementedInterfaces()
            .AsSelf();
    }
}