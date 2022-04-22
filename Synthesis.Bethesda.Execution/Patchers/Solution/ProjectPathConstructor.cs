using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public interface IProjectPathConstructor
{
    FilePath Construct(FilePath solutionPath, string subPath);
}

public class ProjectPathConstructor : IProjectPathConstructor
{
    public FilePath Construct(FilePath solutionPath, string subPath)
    {
        try
        {
            return Path.Combine(solutionPath.Directory!.Value.Path, subPath);
        }
        catch (Exception)
        {
            return default(FilePath);
        }
    }
}