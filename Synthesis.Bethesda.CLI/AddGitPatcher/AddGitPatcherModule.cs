using System.IO.Abstractions;
using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.CLI.AddGitPatcher;

public class AddGitPatcherModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;

    public AddGitPatcherModule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(
            new CommonCliModule(_fileSystem));
        builder.RegisterModule(
            new Synthesis.Bethesda.Execution.Modules.MainModule());

        builder.RegisterType<AddGitPatcherRunner>().AsSelf().SingleInstance();

        // Synthesis.Bethesda.Execution
        builder.RegisterAssemblyTypes(typeof(IPrepareDriverRespository).Assembly)
            .InNamespacesOf(
                typeof(IPrepareDriverRespository),
                typeof(IDriverRepoDirectoryProvider),
                typeof(ISolutionFileLocator),
                typeof(IProfileDirectories))
            .AsSelf()
            .AsMatchingInterface()
            .SingleInstance();
    }
}