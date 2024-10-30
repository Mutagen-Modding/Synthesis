using Mutagen.Bethesda.Environments.DI;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.Projects;

public class CreateTemplatePatcherSolution
{
    private readonly ICreateSolutionFile _createSolutionFile;
    private readonly ICreateProject _createProject;
    private readonly IGameCategoryContext _gameCategoryContext;
    private readonly IAddProjectToSolution _addProjectToSolution;
    private readonly IGenerateGitIgnore _gitIgnore;

    public CreateTemplatePatcherSolution(
        ICreateSolutionFile createSolutionFile,
        ICreateProject createProject,
        IGameCategoryContext gameCategoryContext,
        IAddProjectToSolution addProjectToSolution,
        IGenerateGitIgnore gitIgnore)
    {
        _createSolutionFile = createSolutionFile;
        _createProject = createProject;
        _gameCategoryContext = gameCategoryContext;
        _addProjectToSolution = addProjectToSolution;
        _gitIgnore = gitIgnore;
    }

    public void Create(FilePath solutionPath, FilePath projPath)
    {
        _createSolutionFile.Create(solutionPath);
        _createProject.Create(_gameCategoryContext.Category, projPath);
        _addProjectToSolution.Add(solutionPath, projPath);
        _gitIgnore.Generate(Path.GetDirectoryName(solutionPath)!);
    }
}