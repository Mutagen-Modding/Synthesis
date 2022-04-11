using System.IO;
using System.IO.Abstractions;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.Projects;

public interface IAddProjectToSolution
{
    void Add(
        FilePath solutionpath,
        FilePath projPath);
}

public class AddProjectToSolution : IAddProjectToSolution
{
    private readonly IFileSystem _FileSystem;

    public AddProjectToSolution(
        IFileSystem fileSystem)
    {
        _FileSystem = fileSystem;
    }
        
    public void Add(
        FilePath solutionpath,
        FilePath projPath)
    {
        var projName = Path.GetFileNameWithoutExtension(projPath);
        _FileSystem.File.AppendAllLines(solutionpath,
            $"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"{projName}\", \"{projName}\\{projName}.csproj\", \"{{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}}\"".AsEnumerable()
                .And($"EndProject"));
    }
}