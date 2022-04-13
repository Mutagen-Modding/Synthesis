using System.IO.Abstractions;
using System.Threading;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Noggog.Autofac;
using Noggog.Autofac.Modules;
using Serilog;
using Synthesis.Bethesda.CLI.RunPipeline.Settings;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPipelineModule : Module
{
    public RunPatcherPipelineInstructions Settings { get; }

    public RunPipelineModule(RunPatcherPipelineInstructions settings)
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
            
        builder.Register(_ => CancellationToken.None).AsSelf();
        builder.RegisterInstance(new ConsoleReporter()).As<IRunReporter>();

        builder.RegisterType<PatcherIdProvider>().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopes.RunNickname);
            
        builder.RegisterAssemblyTypes(typeof(ProfileLoadOrderProvider).Assembly)
            .InNamespacesOf(
                typeof(ProfileLoadOrderProvider))
            .NotInNamespacesOf(typeof(DataFolderPathDecorator))
            .AsImplementedInterfaces()
            .AsSelf();
            
        // Settings
        builder.RegisterInstance(Settings)
            .AsSelf()
            .AsImplementedInterfaces();
    }
}