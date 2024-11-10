using System.IO.Abstractions;
using Autofac;

namespace Synthesis.Bethesda.CLI.AddSolutionPatcher;

public class AddSolutionPatcherModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;

    public AddSolutionPatcherModule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(
            new CommonCliModule(_fileSystem));
        builder.RegisterModule(
            new Synthesis.Bethesda.Execution.Modules.MainModule());

        builder.RegisterType<AddSolutionPatcherRunner>().AsSelf().SingleInstance();

        // Synthesis.Bethesda.Execution
        // builder.RegisterAssemblyTypes(typeof(IPrepareDriverRespository).Assembly)
        //     .InNamespacesOf(
        //         typeof(IPrepareDriverRespository),
        //         typeof(IDriverRepoDirectoryProvider),
        //         typeof(ISolutionFileLocator),
        //         typeof(IProfileDirectories))
        //     .AsSelf()
        //     .AsMatchingInterface()
        //     .SingleInstance();
    }
}