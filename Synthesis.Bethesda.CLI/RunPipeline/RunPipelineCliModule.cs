using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Mutagen.Bethesda.Synthesis.Projects;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.CLI.RunPipeline;

/// <summary>
/// Autofac module for CLI pipeline execution.
/// Registers core services needed for running patcher pipelines from the command line.
/// </summary>
public class RunPipelineCliModule : Module
{
    private readonly IFileSystem _fileSystem;
    private readonly RunPatcherPipelineCommand _command;

    public RunPipelineCliModule(
        IFileSystem fileSystem,
        RunPatcherPipelineCommand command)
    {
        _fileSystem = fileSystem;
        _command = command;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<MutagenModule>();
        builder.RegisterModule<Execution.Modules.MainModule>();
        builder.RegisterModule<Execution.Modules.ProfileModule>();
        builder.RegisterModule(new CommonCliModule(_fileSystem));

        builder.Register(_ => CancellationToken.None).AsSelf();
        builder.RegisterType<ErrorClassifier>().As<IErrorClassifier>().SingleInstance();
        builder.RegisterType<ConsoleReporter>().As<IRunReporter>().SingleInstance();

        builder.RegisterType<PatcherIdProvider>().AsImplementedInterfaces()
            .InstancePerMatchingLifetimeScope(LifetimeScopes.RunNickname);

        builder.RegisterAssemblyTypes(typeof(ProfileLoadOrderProvider).Assembly)
            .InNamespacesOf(
                typeof(ProfileLoadOrderProvider))
            .AsImplementedInterfaces()
            .AsSelf()
            .SingleInstance();

        // Mutagen.Bethesda.Synthesis
        builder.RegisterAssemblyTypes(typeof(ProvideCurrentVersions).Assembly)
            .InNamespacesOf(
                typeof(ICreateSolutionFile),
                typeof(ProvideCurrentVersions))
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();

        // Register the command
        builder.RegisterInstance(_command)
            .AsSelf()
            .AsImplementedInterfaces();

        if (_command.DataFolderPath != null)
        {
            builder.RegisterInstance(new DataDirectoryInjection(_command.DataFolderPath))
                .AsImplementedInterfaces();
        }

        if (_command.LoadOrderFilePath != null)
        {
            builder.RegisterInstance(new PluginListingsPathInjection(_command.LoadOrderFilePath))
                .AsImplementedInterfaces();
        }
    }
}
