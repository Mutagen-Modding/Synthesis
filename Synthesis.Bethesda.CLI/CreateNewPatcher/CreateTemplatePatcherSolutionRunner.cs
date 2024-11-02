using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI.CreateNewPatcher;

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
            var cont = new CreateTemplatePatcherSolutionContainer(_fileSystem, Log.Logger, new GameCategoryInjection(cmd.GameCategory));
            var create = cont.Resolve().Value;
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