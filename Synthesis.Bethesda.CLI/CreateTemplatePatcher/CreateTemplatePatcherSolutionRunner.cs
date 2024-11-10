using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis.Projects;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI.CreateTemplatePatcher;

public class CreateTemplatePatcherSolutionRunner
{
    private readonly IFileSystem _fileSystem;

    public CreateTemplatePatcherSolutionRunner(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public async Task<int> Run(CreatePatcherCommand cmd)
    {
        try
        {
            var b = new ContainerBuilder();
            b.RegisterModule(new CreateTemplatePatcherSolutionModule(
                _fileSystem,
                new GameCategoryInjection(cmd.GameCategory)));
            var cont = b.Build();
            var create = cont.Resolve<CreateTemplatePatcherSolution>();
            var solutionPath = Path.Combine(cmd.ParentDirectory, $"{cmd.PatcherName}.sln");
            var projPath = Path.Combine(cmd.ParentDirectory, cmd.PatcherName, $"{cmd.PatcherName}.csproj");
            create.Create(solutionPath, projPath);
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine(ex);
            return -1;
        }
        return 0;
    }
}