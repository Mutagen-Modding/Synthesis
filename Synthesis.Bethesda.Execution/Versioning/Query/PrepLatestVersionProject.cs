using System.IO.Abstractions;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.Projects;
using Noggog.IO;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Versioning.Query;

public interface IPrepLatestVersionProject
{
    Task Prep(CancellationToken cancel);
}

public class PrepLatestVersionProject : IPrepLatestVersionProject
{
    private readonly ILogger _logger;
    private readonly IProcessRunner _processRunner;
    private readonly IDotNetCommandStartConstructor _dotNetCommandStartConstructor;
    public IFileSystem FileSystem { get; }
    public ICreateSolutionFile CreateSolutionFile { get; }
    public ICreateProject CreateProject { get; }
    public IDeleteEntireDirectory DeleteEntireDirectory { get; }
    public IAddProjectToSolution AddProjectToSolution { get; }
    public IQueryVersionProjectPathing Pathing { get; }

    public PrepLatestVersionProject(
        IFileSystem fileSystem,
        ILogger logger,
        ICreateSolutionFile createSolutionFile,
        ICreateProject createProject,
        IDeleteEntireDirectory deleteEntireDirectory,
        IAddProjectToSolution addProjectToSolution,
        IQueryVersionProjectPathing pathing,
        IProcessRunner processRunner,
        IDotNetCommandStartConstructor dotNetCommandStartConstructor)
    {
        _logger = logger;
        _processRunner = processRunner;
        _dotNetCommandStartConstructor = dotNetCommandStartConstructor;
        FileSystem = fileSystem;
        CreateSolutionFile = createSolutionFile;
        CreateProject = createProject;
        DeleteEntireDirectory = deleteEntireDirectory;
        AddProjectToSolution = addProjectToSolution;
        Pathing = pathing;
    }
        
    public async Task Prep(CancellationToken cancel)
    {
        _logger.Information("Prepping Version Query Project");
        DeleteEntireDirectory.DeleteEntireFolder(Pathing.BaseFolder);
        FileSystem.Directory.CreateDirectory(Pathing.BaseFolder);
        CreateSolutionFile.Create(Pathing.SolutionFile);
        CreateProject.Create(GameCategory.Skyrim, Pathing.ProjectFile, insertOldVersion: true, targetFramework: "net5.0");
        AddProjectToSolution.Add(Pathing.SolutionFile, Pathing.ProjectFile);

        _logger.Information("Restoring Version Query Project");
        await _processRunner.Run(
            _dotNetCommandStartConstructor.Construct("restore", Pathing.ProjectFile),
            cancel: cancel).ConfigureAwait(false);
        _logger.Information("Restored Version Query Project");
    }
}