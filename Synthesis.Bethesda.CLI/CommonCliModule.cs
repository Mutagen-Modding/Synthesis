using System.IO.Abstractions;
using Autofac;
using Noggog.Autofac;
using Noggog.Autofac.Modules;
using Serilog;
using Synthesis.Bethesda.CLI.Common;

namespace Synthesis.Bethesda.CLI;

public class CommonCliModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;

    public CommonCliModule(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(_fileSystem).As<IFileSystem>()
            .SingleInstance();
        builder.RegisterModule<NoggogModule>();
        builder.RegisterInstance(Log.Logger).As<ILogger>();
        
        builder.RegisterAssemblyTypes(typeof(PipelineSettingsModifier).Assembly)
            .InNamespacesOf(
                typeof(PipelineSettingsModifier))
            .AsSelf()
            .AsImplementedInterfaces()
            .SingleInstance();
    }
}