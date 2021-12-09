using System.IO;
using Noggog;

namespace Synthesis.Bethesda.Execution.Pathing
{
    public interface IPathSanitation
    {
        string Sanitize(string path);
    }

    public class PathSanitation : IPathSanitation
    {
        public string Sanitize(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}