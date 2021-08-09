using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Cli;

namespace Synthesis.Bethesda.Execution.Modules
{
    public class CliModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(ICliNameConverter).Assembly)
                .InNamespacesOf(
                    typeof(ICliNameConverter))
                .NotInjection()
                .AsImplementedInterfaces();
        }
    }
}