using System.IO.Abstractions;
using Noggog;
using Noggog.IO;
using Noggog.StructuredStrings;

namespace Mutagen.Bethesda.Synthesis.Projects;

public interface ICreateSolutionFile
{
    string[] Create(
        FilePath solutionPath);
}

public class CreateSolutionFile : ICreateSolutionFile
{
    private readonly IFileSystem _fileSystem;
    private readonly IExportStringToFile _exportStringToFile;

    public CreateSolutionFile(
        IFileSystem fileSystem,
        IExportStringToFile exportStringToFile)
    {
        _fileSystem = fileSystem;
        _exportStringToFile = exportStringToFile;
    }
        
    public string[] Create(FilePath solutionPath)
    {
        var slnDir = Path.GetDirectoryName(solutionPath)!;
        _fileSystem.Directory.CreateDirectory(solutionPath.Directory!);

        // Create solution
        StructuredStringBuilder sb = new();
        sb.AppendLine($"Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine($"# Visual Studio Version 17");
        sb.AppendLine($"VisualStudioVersion = 17.10.35122.118");
        sb.AppendLine($"MinimumVisualStudioVersion = 10.0.40219.1");
        _exportStringToFile.ExportToFile(solutionPath, sb.GetString());

        // Create editorconfig
        sb = new StructuredStringBuilder();
        sb.AppendLine("[*]");
        sb.AppendLine("charset = utf-8");
        sb.AppendLine("end_of_line = crlf");
        sb.AppendLine();
        sb.AppendLine("[*.cs]");
        sb.AppendLine();
        sb.AppendLine("# CS4014: Task not awaited");
        sb.AppendLine("dotnet_diagnostic.CS4014.severity = error");
        sb.AppendLine();
        sb.AppendLine("# CS1998: Async function does not contain await");
        sb.AppendLine("dotnet_diagnostic.CS1998.severity = silent");
        _exportStringToFile.ExportToFile(Path.Combine(slnDir, ".editorconfig"), sb.GetString());

        // Add nullability errors
        sb = new StructuredStringBuilder();
        sb.AppendLine("<Project>");
        using (sb.IncreaseDepth())
        {
            sb.AppendLine("<PropertyGroup>");
            using (sb.IncreaseDepth())
            {
                sb.AppendLine("<Nullable>enable</Nullable>");
                sb.AppendLine("<WarningsAsErrors>nullable</WarningsAsErrors>");
            }
            sb.AppendLine("</PropertyGroup>");
        }
        sb.AppendLine("</Project>");
        _exportStringToFile.ExportToFile(Path.Combine(slnDir, "Directory.Build.props"), sb.GetString());

        return new string[]
        {
            solutionPath,
            Path.Combine(slnDir, ".editorconfig"),
            Path.Combine(slnDir, "Directory.Build.props"),
        };
    }
}