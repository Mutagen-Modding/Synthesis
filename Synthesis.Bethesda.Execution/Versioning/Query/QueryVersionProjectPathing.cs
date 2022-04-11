using Noggog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Versioning.Query;

public interface IQueryVersionProjectPathing
{
    DirectoryPath BaseFolder { get; }
    FilePath SolutionFile { get; }
    FilePath ProjectFile { get; }
}

public class QueryVersionProjectPathing : IQueryVersionProjectPathing
{
    public IWorkingDirectoryProvider Paths { get; }
    public DirectoryPath BaseFolder => System.IO.Path.Combine(Paths.WorkingDirectory, "VersionQuery");
    public FilePath SolutionFile => System.IO.Path.Combine(BaseFolder.Path, "VersionQuery.sln");
    public FilePath ProjectFile => System.IO.Path.Combine(BaseFolder.Path, "VersionQuery.csproj");

    public QueryVersionProjectPathing(
        IWorkingDirectoryProvider paths)
    {
        Paths = paths;
    }
}