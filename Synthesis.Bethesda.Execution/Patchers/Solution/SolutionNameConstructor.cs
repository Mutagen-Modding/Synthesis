using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public class SolutionNameConstructor
{
    public string Construct(string nickName, string projectPath)
    {
        try
        {
            if (!nickName.IsNullOrWhitespace()) return nickName;
            var name = Path.GetFileName(Path.GetDirectoryName(projectPath));
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            return name;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}