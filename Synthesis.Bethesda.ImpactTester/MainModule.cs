using System;
using Autofac;
using Noggog.Autofac;
using Noggog.Utility;
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
            builder.RegisterType<ProcessFactory>().As<IProcessFactory>();
            
            builder.RegisterAssemblyTypes(typeof(DotNetVersion).Assembly)
                .InNamespacesOf(
                    typeof(DotNetVersion),
                    typeof(IProcessRunner))
                .AsImplementedInterfaces();
        }
    }
}