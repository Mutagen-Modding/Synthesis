using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface IDefaultDataPathProvider
    {
        DirectoryPath Path { get; }
    }

    public class DefaultDataPathProvider : IDefaultDataPathProvider
    {
        public IPathToProjProvider PathToProjProvider { get; }

        public DirectoryPath Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PathToProjProvider.Path)!, "Data");
        
        public DefaultDataPathProvider(
            IPathToProjProvider pathToProjProvider)
        {
            PathToProjProvider = pathToProjProvider;
        }
    }
}