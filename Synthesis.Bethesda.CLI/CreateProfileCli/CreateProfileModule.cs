using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Synthesis.Profiles;
using Noggog.Autofac;

namespace Synthesis.Bethesda.CLI.CreateProfileCli;

public class CreateProfileModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;

    public CreateProfileModule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(
            new CommonCliModule(_fileSystem));
        builder.RegisterModule(
            new Synthesis.Bethesda.Execution.Modules.MainModule());
        
        builder.RegisterType<CreateProfileRunner>().AsSelf();

        // Synthesis.Bethesda.Execution
        builder.RegisterAssemblyTypes(typeof(CreateProfileId).Assembly)
            .InNamespacesOf(
                typeof(CreateProfileId))
            .AsSelf()
            .AsMatchingInterface()
            .SingleInstance();
    }
}