using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Synthesis.Profiles;
using Noggog.Autofac;
using Synthesis.Bethesda.CLI.Common;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI.CreateProfileCli;

public class CreateProfileModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;
    private readonly CreateProfileCommand _cmd;

    public CreateProfileModule(IFileSystem fileSystem, CreateProfileCommand cmd)
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
        
        builder.RegisterType<CreateProfileRunner>().AsSelf();

        builder.RegisterInstance(_cmd).AsImplementedInterfaces();

        // Mutagen.Bethesda.Synthesis
        builder.RegisterAssemblyTypes(typeof(CreateProfileId).Assembly)
            .InNamespacesOf(
                typeof(CreateProfileId))
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}