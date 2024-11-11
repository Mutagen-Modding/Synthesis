using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog.Autofac;
using Synthesis.Bethesda.CLI.Common;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPipelineModule : Module
{
    private readonly IFileSystem _fileSystem;
    private readonly RunPatcherPipelineCommand _cmd;

    public RunPipelineModule(
        IFileSystem fileSystem,
        RunPatcherPipelineCommand cmd)
    {
        _fileSystem = fileSystem;
        _cmd = cmd;
    }
        
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<MutagenModule>();
        builder.RegisterModule<Execution.Modules.MainModule>();
        builder.RegisterModule<Execution.Modules.ProfileModule>();
        builder.RegisterModule(new CommonCliModule(_fileSystem));
            
        builder.Register(_ => CancellationToken.None).AsSelf();
        builder.RegisterInstance(new ConsoleReporter()).As<IRunReporter>();

        builder.RegisterType<PatcherIdProvider>().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopes.RunNickname);
            
        builder.RegisterAssemblyTypes(typeof(ProfileLoadOrderProvider).Assembly)
            .InNamespacesOf(
                typeof(ProfileLoadOrderProvider),
                typeof(ProfileRetriever))
            .AsImplementedInterfaces()
            .AsSelf();
            
        builder.RegisterInstance(_cmd)
            .AsSelf()
            .AsImplementedInterfaces();
        
        builder.RegisterInstance(new ProfileIdentifier(_cmd.ProfileIdentifier)).AsImplementedInterfaces();
        if (_cmd.DataFolderPath != null)
        {
            builder.RegisterInstance(new DataDirectoryInjection(_cmd.DataFolderPath))
                .AsImplementedInterfaces();
        }

        if (_cmd.LoadOrderFilePath != null)
        {
            builder.RegisterInstance(new PluginListingsPathInjection(_cmd.LoadOrderFilePath))
                .AsImplementedInterfaces();
        }
    }
}