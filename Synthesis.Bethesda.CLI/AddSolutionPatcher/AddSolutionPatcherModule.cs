using System.IO.Abstractions;
using Autofac;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI.AddSolutionPatcher;

public class AddSolutionPatcherModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;
    private readonly AddSolutionPatcherCommand _cmd;

    public AddSolutionPatcherModule(IFileSystem fileSystem, AddSolutionPatcherCommand cmd)
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

        builder.RegisterType<AddSolutionPatcherRunner>().AsSelf().SingleInstance();

        builder.RegisterInstance(_cmd).AsImplementedInterfaces();
    }
}