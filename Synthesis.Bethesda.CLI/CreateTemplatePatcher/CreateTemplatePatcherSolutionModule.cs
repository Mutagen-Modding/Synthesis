using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis.Projects;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog.Autofac;

namespace Synthesis.Bethesda.CLI.CreateTemplatePatcher;

public class CreateTemplatePatcherSolutionModule : Autofac.Module
{
    private readonly IFileSystem _fileSystem;
    private readonly IGameCategoryContext _categoryContext;

    public CreateTemplatePatcherSolutionModule(IFileSystem fileSystem, IGameCategoryContext categoryContext)
    {
        _fileSystem = fileSystem;
        _categoryContext = categoryContext;
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule(
            new CommonCliModule(_fileSystem));
        builder.RegisterModule(
            new Synthesis.Bethesda.Execution.Modules.MainModule());

        builder.RegisterInstance(_categoryContext).AsImplementedInterfaces();
        builder.RegisterType<CreateTemplatePatcherSolution>().AsSelf();

        // Mutagen.Bethesda.Synthesis
        builder.RegisterAssemblyTypes(typeof(ICreateSolutionFile).Assembly)
            .InNamespacesOf(
                typeof(ICreateSolutionFile),
                typeof(IProvideCurrentVersions))
            .AsSelf()
            .AsMatchingInterface()
            .SingleInstance();
    }
}