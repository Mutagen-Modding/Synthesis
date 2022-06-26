using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public interface IPatcherInternalDataPathProvider
{
    DirectoryPath Path { get; }
}

public class PatcherInternalDataPathProvider : IPatcherInternalDataPathProvider
{
    public IPathToProjProvider PathToProjProvider { get; }

    public DirectoryPath Path => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PathToProjProvider.Path)!, "InternalData");
        
    public PatcherInternalDataPathProvider(
        IPathToProjProvider pathToProjProvider)
    {
        PathToProjProvider = pathToProjProvider;
    }
}