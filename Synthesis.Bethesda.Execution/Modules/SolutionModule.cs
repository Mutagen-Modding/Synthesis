using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Modules
{
    public class SolutionModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(IPathToProjProvider).Assembly)
                .InNamespacesOf(
                    typeof(IPathToProjProvider))
                .SingleInstance()
                .NotInjection()
                .AsImplementedInterfaces();
        }
    }
}