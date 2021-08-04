using Autofac;
using Noggog.Autofac;
using Noggog.Autofac.Modules;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.ImpactTester
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.RegisterModule<NoggogModule>();

            builder.RegisterModule<Synthesis.Bethesda.Execution.Modules.MainModule>();
            
            builder.RegisterAssemblyTypes(typeof(DotNetVersion).Assembly)
                .InNamespacesOf(
                    typeof(DotNetVersion),
                    typeof(IProcessRunner))
                .AsImplementedInterfaces();
        }
    }
}