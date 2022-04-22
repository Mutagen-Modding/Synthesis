using System.IO.Abstractions;
using Loqui;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.Projects;

public interface ICreateSolutionFile
{
    string[] Create(
        FilePath solutionPath);
}

public class CreateSolutionFile : ICreateSolutionFile
{
    private readonly IFileSystem _FileSystem;

    public CreateSolutionFile(IFileSystem fileSystem)
    {
        _FileSystem = fileSystem;
    }
        
    public string[] Create(FilePath solutionPath)
    {
        var slnDir = Path.GetDirectoryName(solutionPath)!;
        _FileSystem.Directory.CreateDirectory(solutionPath.Directory);

        // Create solution
        FileGeneration fg = new();
        fg.AppendLine($"Microsoft Visual Studio Solution File, Format Version 12.00");
        fg.AppendLine($"# Visual Studio Version 16");
        fg.AppendLine($"VisualStudioVersion = 16.0.30330.147");
        fg.AppendLine($"MinimumVisualStudioVersion = 10.0.40219.1");
        fg.Generate(solutionPath);

        // Create editorconfig
        fg = new FileGeneration();
        fg.AppendLine("[*]");
        fg.AppendLine("charset = utf-8");
        fg.AppendLine("end_of_line = crlf");
        fg.AppendLine();
        fg.AppendLine("[*.cs]");
        fg.AppendLine();
        fg.AppendLine("# CS4014: Task not awaited");
        fg.AppendLine("dotnet_diagnostic.CS4014.severity = error");
        fg.AppendLine();
        fg.AppendLine("# CS1998: Async function does not contain await");
        fg.AppendLine("dotnet_diagnostic.CS1998.severity = silent");
        fg.Generate(Path.Combine(slnDir, ".editorconfig"));

        // Add nullability errors
        fg = new FileGeneration();
        fg.AppendLine("<Project>");
        using (new DepthWrapper(fg))
        {
            fg.AppendLine("<PropertyGroup>");
            using (new DepthWrapper(fg))
            {
                fg.AppendLine("<Nullable>enable</Nullable>");
                fg.AppendLine("<WarningsAsErrors>nullable</WarningsAsErrors>");
            }
            fg.AppendLine("</PropertyGroup>");
        }
        fg.AppendLine("</Project>");
        fg.Generate(Path.Combine(slnDir, "Directory.Build.props"), fileSystem: _FileSystem);

        return new string[]
        {
            solutionPath,
            Path.Combine(slnDir, ".editorconfig"),
            Path.Combine(slnDir, "Directory.Build.props"),
        };
    }
}