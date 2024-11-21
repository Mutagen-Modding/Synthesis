using System.IO.Abstractions;
using Autofac;
using Noggog.Autofac;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Profile.Services;

namespace Synthesis.Bethesda.CLI.AddGitPatcher;

public class AddGitPatcherModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;
    private readonly AddGitPatcherCommand _cmd;

    public AddGitPatcherModule(IFileSystem fileSystem, AddGitPatcherCommand cmd)
    {
        _fileSystem = fileSystem;
        _cmd = cmd;
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(
            new CommonCliModule(_fileSystem));
        builder.RegisterModule(
            new Synthesis.Bethesda.Execution.Modules.MainModule());

        builder.RegisterType<AddGitPatcherRunner>().AsSelf().SingleInstance();

        builder.RegisterInstance(_cmd).AsImplementedInterfaces();

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