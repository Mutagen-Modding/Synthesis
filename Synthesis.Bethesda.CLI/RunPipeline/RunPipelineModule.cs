using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Noggog.Autofac;
using Noggog.Autofac.Modules;
using Serilog;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPipelineModule : Module
{
    public RunPatcherPipelineCommand Settings { get; }

    public RunPipelineModule(RunPatcherPipelineCommand settings)
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
            .AsImplementedInterfaces()
            .AsSelf();
            
        // Settings
        builder.RegisterInstance(Settings)
            .AsSelf()
            .AsImplementedInterfaces();
    }
}