using Loqui;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public static class SolutionInitialization
    {
        public static GetResponse<string> ValidateProjectPath(string projName, GetResponse<string> sln)
        {
            if (string.IsNullOrWhiteSpace(projName)) return GetResponse<string>.Fail("Project needs a name.");
            if (!StringExt.IsViableFilename(projName)) return GetResponse<string>.Fail($"Project had invalid path characters.");
            if (projName.IndexOf(' ') != -1) return GetResponse<string>.Fail($"Project name cannot contain spaces.");

            // Just mark as success until we have one and can analyze further
            if (sln.Failed) return GetResponse<string>.Succeed(string.Empty);

            try
            {
                var projPath = Path.Combine(Path.GetDirectoryName(sln.Value)!, projName, $"{projName}.csproj");
                if (File.Exists(projPath))
                {
                    return GetResponse<string>.Fail($"Target project folder cannot already exist as a file: {projPath}");
                }
                if (Directory.Exists(projPath)
                    && (Directory.EnumerateFiles(projPath).Any()
                    || Directory.EnumerateDirectories(projPath).Any()))
                {
                    return GetResponse<string>.Fail($"Target project folder must be empty: {projPath}");
                }
                return GetResponse<string>.Succeed(projPath);
            }
            catch (ArgumentException)
            {
                return GetResponse<string>.Fail("Improper project name. Go simpler.");
            }
        }

        public static string[] CreateProject(string projPath, GameCategory category)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
            var projName = Path.GetFileNameWithoutExtension(projPath);

            // Generate Project File
            FileGeneration fg = new FileGeneration();
            fg.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            using (new DepthWrapper(fg))
            {
                fg.AppendLine($"<PropertyGroup>");
                using (new DepthWrapper(fg))
                {
                    fg.AppendLine($"<OutputType>Exe</OutputType>");
                    fg.AppendLine($"<TargetFramework>netcoreapp3.1</TargetFramework>");
                }
                fg.AppendLine($"</PropertyGroup>");
                fg.AppendLine();

                fg.AppendLine($"<ItemGroup>");
                using (new DepthWrapper(fg))
                {
                    fg.AppendLine($"<PackageReference Include=\"Mutagen.Bethesda\" Version=\"{Versions.MutagenVersion}\" />");
                    fg.AppendLine($"<PackageReference Include=\"Mutagen.Bethesda.Synthesis\" Version=\"{Versions.SynthesisVersion}\" />");
                }
                fg.AppendLine($"</ItemGroup>");
                fg.AppendLine();
            }
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
                            fg.AppendLine($".Run(args, new {nameof(RunPreferences)}()");
                            using (new BraceWrapper(fg) { AppendParenthesis = true, AppendSemicolon = true })
                            {
                                fg.AppendLine($"{nameof(UserPreferences.ActionsForEmptyArgs)} = new {nameof(RunDefaultPatcher)}()");
                                using (new BraceWrapper(fg))
                                {
                                    fg.AppendLine($"{nameof(RunDefaultPatcher.IdentifyingModKey)} = \"YourPatcher.esp\",");
                                    fg.AppendLine($"{nameof(RunDefaultPatcher.TargetRelease)} = {nameof(GameRelease)}.{category.DefaultRelease()},");
                                }
                            }
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

        public static string[] CreateSolutionFile(string solutionPath)
        {
            var slnDir = Path.GetDirectoryName(solutionPath)!;
            Directory.CreateDirectory(slnDir);

            // Create solution
            FileGeneration fg = new FileGeneration();
            fg.AppendLine($"Microsoft Visual Studio Solution File, Format Version 12.00");
            fg.AppendLine($"# Visual Studio Version 16");
            fg.AppendLine($"VisualStudioVersion = 16.0.30330.147");
            fg.AppendLine($"MinimumVisualStudioVersion = 10.0.40219.1");
            fg.Generate(solutionPath);

            // Create editorconfig
            fg = new FileGeneration();
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
            fg.Generate(Path.Combine(slnDir, "Directory.Build.props"));

            return new string[]
            {
                solutionPath,
                Path.Combine(slnDir, ".editorconfig"),
                Path.Combine(slnDir, "Directory.Build.props"),
            };
        }

        public static void AddProjectToSolution(string solutionpath, string projPath)
        {
            var projName = Path.GetFileNameWithoutExtension(projPath);
            File.AppendAllLines(solutionpath,
                $"Project(\"{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}\") = \"{projName}\", \"{projName}\\{projName}.csproj\", \"{{8BA2E1B7-DD65-42B6-A780-17E4037A3C1B}}\"".AsEnumerable()
                .And($"EndProject"));
        }
    }
}
