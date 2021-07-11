using Autofac;
using Noggog.Autofac;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.CLI
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProcessFactory>().As<IProcessFactory>();
            builder.RegisterAssemblyTypes(typeof(ICheckRunnability).Assembly)
                .AsMatchingInterface();
        }
    }
}