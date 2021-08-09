using System.IO.Abstractions;
using System.Threading;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog.Autofac;
using Noggog.Autofac.Modules;
using Serilog;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Running;

namespace Synthesis.Bethesda.CLI
{
    public class MainModule : Module
    {
        public RunPatcherPipelineInstructions Settings { get; }

        public MainModule(RunPatcherPipelineInstructions settings)
        {
            Settings = settings;
        }
        
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<MutagenModule>();
            builder.RegisterType<FileSystem>().As<IFileSystem>();
            builder.RegisterModule<NoggogModule>();
            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.RegisterAssemblyTypes(typeof(IExecuteRunnabilityCheck).Assembly)
                .AsMatchingInterface();
            
            builder.Register(_ => CancellationToken.None).AsSelf();
            builder.RegisterInstance(new ConsoleReporter()).As<IRunReporter>();
            
            // Settings
            builder.RegisterInstance(Settings)
                .AsImplementedInterfaces();
        }
    }
}