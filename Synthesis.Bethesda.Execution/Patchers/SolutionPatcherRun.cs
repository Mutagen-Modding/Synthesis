using Buildalyzer;
using CommandLine;
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using Noggog.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.Patchers
{
    public class SolutionPatcherRun : IPatcherRun
    {
        public string Name { get; }
        public string PathToSolution { get; }
        public string PathToProject { get; }
        public string PathToExtraDataBaseFolder { get; }

        private readonly Subject<string> _output = new();
        public IObservable<string> Output => _output;

        private readonly Subject<string> _error = new();
        public IObservable<string> Error => _error;

        public SolutionPatcherRun(
            string name,
            string pathToSln, 
            string pathToProj,
            string pathToExtraDataBaseFolder)
        {
            PathToSolution = pathToSln;
            PathToProject = pathToProj;
            PathToExtraDataBaseFolder = pathToExtraDataBaseFolder;
            Name = name;
        }

        public async Task Prep(GameRelease release, CancellationToken cancel)
        {
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    _output.OnNext($"Compiling");
                    var resp = await DotNetCommands.Compile(PathToProject, cancel, _output.OnNext).ConfigureAwait(false);
                    if (!resp.Succeeded)
                    {
                        throw new SynthesisBuildFailure(resp.Reason);
                    }
                }),
                Task.Run(async () =>
                {
                    await CopyOverExtraData().ConfigureAwait(false);
                })).ConfigureAwait(false); ;
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
        {
            var repoPath = Path.GetDirectoryName(PathToSolution);
            if (Repository.IsValid(repoPath))
            {
                using var repo = new Repository(repoPath);
                _output.OnNext($"Sha {repo.Head.Tip.Sha}");
            }
            
            var runnability = await Synthesis.Bethesda.Execution.CLI.Commands.CheckRunnability(
                PathToProject,
                directExe: false,
                release: settings.GameRelease,
                dataFolder: settings.DataFolderPath,
                loadOrderPath: settings.LoadOrderFilePath,
                cancel: cancel);

            if (runnability.Failed)
            {
                throw new CliUnsuccessfulRunException((int)Codes.NotRunnable, runnability.Reason);
            }

            var defaultDataFolderPath = GetDefaultDataPathFromProj(PathToProject);

            var internalSettings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = settings.DataFolderPath,
                ExtraDataFolder = Path.Combine(PathToExtraDataBaseFolder, Name),
                DefaultDataFolderPath = Directory.Exists(defaultDataFolderPath) ? defaultDataFolderPath : null,
                GameRelease = settings.GameRelease,
                LoadOrderFilePath = settings.LoadOrderFilePath,
                OutputPath = settings.OutputPath,
                SourcePath = settings.SourcePath,
                PatcherName = settings.PatcherName,
                PersistencePath = settings.PersistencePath
            };
            var args = Parser.Default.FormatCommandLine(internalSettings);
            using var process = ProcessWrapper.Create(
                new ProcessStartInfo("dotnet", $"run --project \"{PathToProject}\" --runtime win-x64 --no-build {args}"),
                cancel: cancel);
            _output.OnNext("Running");
            _output.OnNext($"({process.StartInfo.WorkingDirectory}): {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            using var outputSub = process.Output.Subscribe(_output);
            using var errSub = process.Error.Subscribe(_error);
            var result = await process.Run().ConfigureAwait(false);
            if (result != 0)
            {
                throw new CliUnsuccessfulRunException(result, "Error running solution patcher");
            }
        }

        public void Dispose()
        {
        }

        // Almost there, I think, but not currently working.
        public static async Task<(bool OverallSuccess, EmitResult? TriggeringFailure)> CompileWithRosyln(string solutionUrl, string outputDir, CancellationToken cancel)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = workspace.OpenSolutionAsync(solutionUrl).Result;
            ProjectDependencyGraph projectGraph = solution.GetProjectDependencyGraph();

            foreach (ProjectId projectId in projectGraph.GetTopologicallySortedProjects())
            {
                Compilation? compilation = await solution.GetProject(projectId)!.GetCompilationAsync();
                if (compilation == null || string.IsNullOrEmpty(compilation.AssemblyName))
                {
                    return (false, default);
                }
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
                compilation = compilation.WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication));
                if (!string.IsNullOrEmpty(compilation.AssemblyName))
                {
                    using var stream = new MemoryStream();
                    EmitResult result = compilation.Emit(stream);
                    if (result.Success)
                    {
                        string fileName = string.Format("{0}.dll", compilation.AssemblyName);

                        using FileStream file = File.Create(Path.Combine(outputDir, fileName));
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.CopyTo(file);
                    }
                    else
                    {
                        return (false, result);
                    }
                }
            }

            return (true, default);
        }

        public static IEnumerable<string> AvailableProjects(string solutionPath)
        {
            if (!File.Exists(solutionPath)) return Enumerable.Empty<string>();
            try
            {
                var manager = new AnalyzerManager(solutionPath);
                return manager.Projects.Keys.Select(projPath => projPath.TrimStart($"{Path.GetDirectoryName(solutionPath)}\\"!));
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }

        public static string? AvailableProject(string solutionPath, string projSubpath)
        {
            var projName = Path.GetFileName(projSubpath);
            return AvailableProjects(solutionPath)
                .Where(av => Path.GetFileName(av).Equals(projName))
                .FirstOrDefault();
        }

        private Task CopyOverExtraData()
        {
            return CopyOverExtraData(PathToProject, PathToExtraDataBaseFolder, Name, _output.OnNext);
        }

        public static string GetDefaultDataPathFromProj(string pathToProject)
        {
            return Path.Combine(Path.GetDirectoryName(pathToProject)!, "Data");
        }

        public static async Task CopyOverExtraData(string pathToProject, string pathToExtraDataBaseFolder, string name, Action<string> log)
        {
            var inputExtraData = new DirectoryInfo(GetDefaultDataPathFromProj(pathToProject));
            if (!inputExtraData.Exists)
            {
                log("No extra data to consider.");
                return;
            }

            var outputExtraData = new DirectoryInfo(Path.Combine(pathToExtraDataBaseFolder, name));
            if (outputExtraData.Exists)
            {
                log($"Extra data folder already exists. Leaving as is: {outputExtraData}");
                return;
            }

            log("Copying extra data folder");
            log($"  From: {inputExtraData}");
            log($"  To: {outputExtraData}");
            inputExtraData.DeepCopy(outputExtraData);
        }

        public static IEnumerable<string> AvailableProjectSubpaths(string solutionPath)
        {
            if (!File.Exists(solutionPath)) return Enumerable.Empty<string>();
            try
            {
                var manager = new AnalyzerManager(solutionPath);
                return manager.Projects.Keys.Select(projPath => projPath.TrimStart($"{Path.GetDirectoryName(solutionPath)}\\"!));
            }
            catch (Exception)
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}
