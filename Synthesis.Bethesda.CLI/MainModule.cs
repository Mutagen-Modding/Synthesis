using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Noggog.Autofac;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution.CLI;

namespace Synthesis.Bethesda.CLI
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<MutagenModule>();
            builder.RegisterType<FileSystem>().As<IFileSystem>();
            builder.RegisterType<ProcessFactory>().As<IProcessFactory>();
            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.RegisterAssemblyTypes(typeof(ICheckRunnability).Assembly)
                .AsMatchingInterface();
        }
    }
}