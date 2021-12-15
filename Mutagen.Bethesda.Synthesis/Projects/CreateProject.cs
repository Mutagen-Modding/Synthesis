using System.IO;
using System.IO.Abstractions;
using Loqui;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;

namespace Mutagen.Bethesda.Synthesis.Projects
{
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
        private readonly IFileSystem _FileSystem;

        public CreateProject(
            IFileSystem fileSystem)
        {
            _FileSystem = fileSystem;
        }
        
        public string[] Create(
            GameCategory category,
            FilePath projPath,
            bool insertOldVersion = false,
            string? targetFramework = null)
        {
            _FileSystem.Directory.CreateDirectory(projPath.Directory);
            var projName = projPath.NameWithoutExtension;

            // Generate Project File
            FileGeneration fg = new();
            fg.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            fg.AppendLine($"  <PropertyGroup>");
            fg.AppendLine($"    <OutputType>Exe</OutputType>");
            fg.AppendLine($"    <TargetFramework>{(targetFramework ?? "net6.0")}</TargetFramework>");
            fg.AppendLine($"    <TargetPlatformIdentifier>Windows</TargetPlatformIdentifier>");
            fg.AppendLine($"  </PropertyGroup>");
            fg.AppendLine();
            fg.AppendLine($"  <ItemGroup>");
            fg.AppendLine($"    <PackageReference Include=\"Mutagen.Bethesda\" Version=\"{(insertOldVersion ? Versions.OldMutagenVersion : Versions.MutagenVersion)}\" />");
            fg.AppendLine($"    <PackageReference Include=\"Mutagen.Bethesda.Synthesis\" Version=\"{(insertOldVersion ? Versions.OldSynthesisVersion : Versions.SynthesisVersion)}\" />");
            fg.AppendLine($"  </ItemGroup>");
            fg.AppendLine("</Project>");
            fg.Generate(projPath);

            // Generate Program.cs
            fg = new FileGeneration();
            fg.AppendLine("using System;");
            fg.AppendLine("using System.Collections.Generic;");
            fg.AppendLine("using System.Linq;");
            fg.AppendLine("using Mutagen.Bethesda;");
            fg.AppendLine("using Mutagen.Bethesda.Synthesis;");
            fg.AppendLine($"using Mutagen.Bethesda.{category};");
            fg.AppendLine("using System.Threading.Tasks;");
            fg.AppendLine(); 
            fg.AppendLine($"namespace {projName}");
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"public class Program");
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine("public static async Task<int> Main(string[] args)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"return await SynthesisPipeline.Instance");
                        using (new DepthWrapper(fg))
                        {
                            fg.AppendLine($".AddPatch<I{category}Mod, I{category}ModGetter>(RunPatch)");
                            fg.AppendLine($".SetTypicalOpen({nameof(GameRelease)}.{category.DefaultRelease()}, \"YourPatcher.esp\")");
                            fg.AppendLine($".Run(args);");
                        }
                    }
                    fg.AppendLine();

                    fg.AppendLine($"public static void RunPatch({nameof(IPatcherState)}<I{category}Mod, I{category}ModGetter> state)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"//Your code here!");
                    }
                }
            }
            fg.Generate(Path.Combine(Path.GetDirectoryName(projPath)!, "Program.cs"));

            return new string[]
            {
                projPath,
                Path.Combine(Path.GetDirectoryName(projPath)!, "Program.cs")
            };
        }
    }
}