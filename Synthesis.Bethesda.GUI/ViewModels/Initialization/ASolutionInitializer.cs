using Loqui;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public abstract class ASolutionInitializer : ViewModel
    {
        public delegate Task<IEnumerable<SolutionPatcherVM>> InitializerCall(ProfileVM profile);
        public abstract IObservable<GetResponse<InitializerCall>> InitializationCall { get; }

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

        public static void CreateProject(string projPath, GameCategory category)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(projPath));
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
                    fg.AppendLine($"<PackageReference Include=\"Mutagen.Bethesda\" Version=\"{typeof(IMod).Assembly.GetName().Version}\" />");
                    fg.AppendLine($"<PackageReference Include=\"Mutagen.Bethesda.Synthesis\" Version=\"{typeof(SynthesisPipeline).Assembly.GetName().Version}\" />");
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
            fg.AppendLine();
            fg.AppendLine($"namespace {projName}");
            using (new BraceWrapper(fg))
            {
                fg.AppendLine($"public class Program");
                using (new BraceWrapper(fg))
                {
                    fg.AppendLine("public static int Main(string[] args)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"return SynthesisPipeline.Instance.Patch<I{category}Mod, I{category}ModGetter>(");
                        using (new DepthWrapper(fg))
                        {
                            fg.AppendLine("args: args,");
                            fg.AppendLine("patcher: RunPatch);");
                        }
                    }
                    fg.AppendLine();

                    fg.AppendLine($"public static void RunPatch(SynthesisState<I{category}Mod, I{category}ModGetter> state)");
                    using (new BraceWrapper(fg))
                    {
                        fg.AppendLine($"//Your code here!");
                    }
                }
            }
            fg.Generate(Path.Combine(Path.GetDirectoryName(projPath)!, "Program.cs"));
        }

        public static void CreateSolutionFile(string solutionPath)
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
