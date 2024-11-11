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
        var str = 
	        $$"""
	          Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "{{projName}}", "{{projName}}\{{projName}}.csproj", "{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}"
	          EndProject
	          Global
	          	GlobalSection(SolutionConfigurationPlatforms) = preSolution
	          		Debug|Any CPU = Debug|Any CPU
	          		Release|Any CPU = Release|Any CPU
	          	EndGlobalSection
	          	GlobalSection(ProjectConfigurationPlatforms) = postSolution
	          		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
	          		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Debug|Any CPU.Build.0 = Debug|Any CPU
	          		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Release|Any CPU.ActiveCfg = Release|Any CPU
	          		{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}.Release|Any CPU.Build.0 = Release|Any CPU
	          	EndGlobalSection
	          	GlobalSection(SolutionProperties) = preSolution
	          		HideSolutionNode = FALSE
	          	EndGlobalSection
	          EndGlobal
	          """;
        _FileSystem.File.AppendAllText(solutionpath.Path, str);
    }
}