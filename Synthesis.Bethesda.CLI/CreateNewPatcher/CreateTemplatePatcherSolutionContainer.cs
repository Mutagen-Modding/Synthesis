using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis.Projects;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog.IO;
using Serilog;
using StrongInject;

namespace Synthesis.Bethesda.CLI.CreateNewPatcher;

[Register(typeof(ExportStringToFile), typeof(IExportStringToFile))]
[Register(typeof(ProvideCurrentVersions), typeof(IProvideCurrentVersions))]
[Register(typeof(CreateSolutionFile), typeof(ICreateSolutionFile))]
[Register(typeof(CreateProject), typeof(ICreateProject))]
[Register(typeof(GenerateGitIgnore), typeof(IGenerateGitIgnore))]
[Register(typeof(AddProjectToSolution), typeof(IAddProjectToSolution))]
[Register(typeof(CreateTemplatePatcherSolution))]
public partial class CreateTemplatePatcherSolutionContainer : IContainer<CreateTemplatePatcherSolution>
{
    [Instance] private readonly IFileSystem _fileSystem;
    [Instance] private readonly ILogger _logger;
    [Instance] private readonly IGameCategoryContext _gameCategoryContext;

    public CreateTemplatePatcherSolutionContainer(
        IFileSystem fileSystem,
        ILogger logger,
        IGameCategoryContext gameCategoryContext)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _gameCategoryContext = gameCategoryContext;
    }
}