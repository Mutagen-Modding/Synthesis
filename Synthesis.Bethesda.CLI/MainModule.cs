using System.IO.Abstractions;
using System.Threading;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog.Autofac;
using Noggog.Utility; 
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
            builder.RegisterType<ProcessFactory>().As<IProcessFactory>();
            builder.RegisterInstance(Log.Logger).As<ILogger>();
            builder.RegisterAssemblyTypes(typeof(ICheckRunnability).Assembly)
                .AsMatchingInterface();
            
            builder.Register(_ => CancellationToken.None).AsSelf();
            builder.RegisterInstance(new ConsoleReporter()).As<IRunReporter>();
            
            // Settings
            builder.RegisterInstance(new GameReleaseInjection(Settings.GameRelease))
                .As<IGameReleaseContext>();
            builder.RegisterInstance(new DataDirectoryInjection(Settings.DataFolderPath))
                .As<IDataDirectoryProvider>();
            builder.RegisterInstance(new PluginListingsPathInjection(Settings.LoadOrderFilePath))
                .As<IPluginListingsPathProvider>();
            builder.RegisterInstance(new ProfileDefinitionPathInjection { Path = Settings.ProfileDefinitionPath })
                .As<IProfileDefinitionPathProvider>();
            builder.RegisterInstance(new ProfileNameInjection { Name = Settings.ProfileName })
                .As<IProfileNameProvider>();
        }
    }
}