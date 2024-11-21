using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public interface IProjectPathConstructor
{
    FilePath Construct(FilePath solutionPath, string subPath);
}

public class ProjectPathConstructor : IProjectPathConstructor
{
    private readonly string[] _directories = new[] { "Driver", "Runner" };

    public FilePath Construct(FilePath solutionPath, string subPath)
    {
        try
        {
            if (Path.IsPathRooted(subPath))
            {
                foreach (var dir in _directories)
                {
                    var index = subPath.IndexOf(dir, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        var before = subPath;
                        subPath = subPath.Substring(index + dir.Length + 1);
                        break;
                    }
                }
                
            }
            return Path.Combine(solutionPath.Directory!.Value.Path, subPath);
        }
        catch (Exception)
        {
            return default(FilePath);
        }
    }
}
