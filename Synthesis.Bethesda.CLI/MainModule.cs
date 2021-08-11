using System.IO.Abstractions;
using System.Threading;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Noggog.Autofac.Modules;
using Serilog;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Running.Cli.Settings;

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
            builder.RegisterModule<Execution.Modules.MainModule>();
            builder.RegisterModule<Execution.Modules.ProfileModule>();
            builder.RegisterModule<Execution.Modules.PatcherModule>();
            
            builder.Register(_ => CancellationToken.None).AsSelf();
            builder.RegisterInstance(new ConsoleReporter()).As<IRunReporter>();
            
            // Settings
            builder.RegisterInstance(Settings)
                .AsSelf()
                .AsImplementedInterfaces();
        }
    }
}