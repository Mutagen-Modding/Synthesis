using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;

namespace Synthesis.Bethesda.Execution.Modules
{
    public class GitPatcherModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<SolutionPatcherModule>();
            
            builder.RegisterAssemblyTypes(typeof(IGitPatcherRun).Assembly)
                .InNamespacesOf(
                    typeof(IGitPatcherRun))
                .NotInjection()
                .AsImplementedInterfaces();
        }
    }
}