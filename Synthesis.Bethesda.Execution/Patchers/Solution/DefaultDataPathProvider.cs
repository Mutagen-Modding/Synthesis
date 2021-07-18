using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface IDefaultDataPathProvider
    {
        DirectoryPath Path { get; }
    }

    public class DefaultDataPathProvider : IDefaultDataPathProvider
    {
        private readonly IPathToProjProvider _pathToProjProvider;

        public DirectoryPath Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_pathToProjProvider.Path)!, "Data");
        
        public DefaultDataPathProvider(
            IPathToProjProvider pathToProjProvider)
        {
            _pathToProjProvider = pathToProjProvider;
        }
    }
}