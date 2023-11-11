using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.Patchers.Solution;

public interface IAvailableProjectsRetriever
{
    IEnumerable<string> Get(FilePath solutionPath);
}

[ExcludeFromCodeCoverage]
public class AvailableProjectsRetriever : IAvailableProjectsRetriever
{
    private readonly ILogger _logger;
    public IFileSystem FileSystem { get; }

    public AvailableProjectsRetriever(
        ILogger logger,
        IFileSystem fileSystem)
    {
        _logger = logger;
        FileSystem = fileSystem;
    }
        
    public IEnumerable<string> Get(FilePath solutionPath)
    {
        if (!FileSystem.File.Exists(solutionPath))
        {
            yield break;
        }
        
        foreach (var line in FileSystem.File.ReadLines(solutionPath))
        {
            if (!line.StartsWith("Project(")) continue;
            var indexOfComma = line.IndexOf(",");
            if (indexOfComma == -1) continue;
            var laterIndexOfComma = line.IndexOf(",", indexOfComma + 1);
            if (laterIndexOfComma == -1) continue;
            var projSpan = line.AsSpan().Slice(indexOfComma + 1, laterIndexOfComma - indexOfComma - 1).Trim();
            projSpan = projSpan.TrimStart("\"").TrimEnd("\"");
            if (!projSpan.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) continue;
            yield return Path.Combine(Path.GetDirectoryName(solutionPath)!, projSpan.ToString());
        }
    }
}