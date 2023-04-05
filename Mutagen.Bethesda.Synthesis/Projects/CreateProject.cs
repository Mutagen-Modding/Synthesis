using System.IO.Abstractions;
using Loqui;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.IO;
using Noggog.StructuredStrings;
using Noggog.StructuredStrings.CSharp;

namespace Mutagen.Bethesda.Synthesis.Projects;

public interface ICreateProject
{
    string[] Create(
        GameCategory category,
        FilePath projPath,
        bool insertOldVersion = false,
        string? targetFramework = null);
}

public class CreateProject : ICreateProject
{
    private readonly IFileSystem _fileSystem;
    private readonly IProvideCurrentVersions _currentVersions;
    private readonly IExportStringToFile _exportStringToFile;

    public CreateProject(
        IFileSystem fileSystem,
        IProvideCurrentVersions currentVersions,
        IExportStringToFile exportStringToFile)
    {
        _fileSystem = fileSystem;
        _currentVersions = currentVersions;
        _exportStringToFile = exportStringToFile;
    }
        
    public string[] Create(
        GameCategory category,
        FilePath projPath,
        bool insertOldVersion = false,
        string? targetFramework = null)
    {
        _fileSystem.Directory.CreateDirectory(projPath.Directory!);
        var projName = projPath.NameWithoutExtension;

        // Generate Project File
        StructuredStringBuilder sb = new();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine($"  <PropertyGroup>");
        sb.AppendLine($"    <OutputType>Exe</OutputType>");
        sb.AppendLine($"    <TargetFramework>{(targetFramework ?? "net6.0")}</TargetFramework>");
        sb.AppendLine($"    <TargetPlatformIdentifier>Windows</TargetPlatformIdentifier>");
        sb.AppendLine($"    <ImplicitUsings>true</ImplicitUsings>");
        sb.AppendLine($"  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine($"  <ItemGroup>");
        sb.AppendLine($"    <PackageReference Include=\"Mutagen.Bethesda.{category}\" Version=\"{(insertOldVersion ? _currentVersions.OldMutagenVersion : _currentVersions.MutagenVersion)}\" />");
        sb.AppendLine($"    <PackageReference Include=\"Mutagen.Bethesda.Synthesis\" Version=\"{(insertOldVersion ? _currentVersions.OldSynthesisVersion : _currentVersions.SynthesisVersion)}\" />");
        sb.AppendLine($"  </ItemGroup>");
        sb.AppendLine("</Project>");
        _exportStringToFile.ExportToFile(projPath, sb.GetString());

        // Generate Program.cs
        sb = new StructuredStringBuilder();
        sb.AppendLine("using Mutagen.Bethesda;");
        sb.AppendLine("using Mutagen.Bethesda.Synthesis;");
        sb.AppendLine($"using Mutagen.Bethesda.{category};");
        sb.AppendLine(); 
        sb.AppendLine($"namespace {projName}");
        using (sb.CurlyBrace())
        {
            sb.AppendLine($"public class Program");
            using (sb.CurlyBrace())
            {
                sb.AppendLine("public static async Task<int> Main(string[] args)");
                using (sb.CurlyBrace())
                {
                    sb.AppendLine($"return await SynthesisPipeline.Instance");
                    using (sb.IncreaseDepth())
                    {
                        sb.AppendLine($".AddPatch<I{category}Mod, I{category}ModGetter>(RunPatch)");
                        sb.AppendLine($".SetTypicalOpen({nameof(GameRelease)}.{category.DefaultRelease()}, \"YourPatcher.esp\")");
                        sb.AppendLine($".Run(args);");
                    }
                }
                sb.AppendLine();

                sb.AppendLine($"public static void RunPatch({nameof(IPatcherState)}<I{category}Mod, I{category}ModGetter> state)");
                using (sb.CurlyBrace())
                {
                    sb.AppendLine($"//Your code here!");
                }
            }
        }
        _exportStringToFile.ExportToFile(Path.Combine(Path.GetDirectoryName(projPath)!, "Program.cs"), sb.GetString());

        return new string[]
        {
            projPath,
            Path.Combine(Path.GetDirectoryName(projPath)!, "Program.cs")
        };
    }
}