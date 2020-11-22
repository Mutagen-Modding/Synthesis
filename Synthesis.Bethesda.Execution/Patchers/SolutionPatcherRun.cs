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

        private Subject<string> _output = new Subject<string>();
        public IObservable<string> Output => _output;

        private Subject<string> _error = new Subject<string>();
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

        public async Task Prep(GameRelease release, CancellationToken? cancel = null)
        {
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    _output.OnNext($"Compiling");
                    var resp = await CompileWithDotnet(PathToProject, cancel ?? CancellationToken.None).ConfigureAwait(false);
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

        public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
        {
            var repoPath = Path.GetDirectoryName(PathToSolution);
            if (Repository.IsValid(repoPath))
            {
                using var repo = new Repository(repoPath);
                _output.OnNext($"Sha {repo.Head.Tip.Sha}");
            }
            var internalSettings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = settings.DataFolderPath,
                ExtraDataFolder = Path.Combine(PathToExtraDataBaseFolder, Name),
                GameRelease = settings.GameRelease,
                LoadOrderFilePath = settings.LoadOrderFilePath,
                OutputPath = settings.OutputPath,
                SourcePath = settings.SourcePath
            };
            var args = Parser.Default.FormatCommandLine(internalSettings);
            using var process = ProcessWrapper.Start(
                new ProcessStartInfo("dotnet", $"run --project \"{PathToProject}\" --runtime win-x64 --no-build {args}"),
                cancel: cancel);
            using var outputSub = process.Output.Subscribe(_output);
            using var errSub = process.Error.Subscribe(_error);
            var result = await process.Start().ConfigureAwait(false);
            if (result != 0)
            {
                throw new CliUnsuccessfulRunException(result, "Error running solution patcher");
            }
        }

        public void Dispose()
        {
        }

        // Almost there, I think, but not currently working.
        public static async Task<(bool OverallSuccess, EmitResult? TriggeringFailure)> CompileWithRosyln(string solutionUrl, CancellationToken cancel, string outputDir)
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

        public static async Task<ErrorResponse> CompileWithDotnet(string targetPath, CancellationToken cancel)
        {
            using var process = ProcessWrapper.Start(
                new ProcessStartInfo("dotnet", $"build --runtime win-x64 \"{Path.GetFileName(targetPath)}\"")
                {
                    WorkingDirectory = Path.GetDirectoryName(targetPath)
                },
                cancel: cancel);
            string? firstError = null;
            bool buildFailed = false;
            process.Output.Subscribe(o =>
            {
                if (o.StartsWith("Build FAILED"))
                {
                    buildFailed = true;
                }
                else if (buildFailed 
                    && firstError == null
                    && !string.IsNullOrWhiteSpace(o))
                {
                    firstError = o;
                }
            });
            var result = await process.Start().ConfigureAwait(false);
            if (result == 0) return ErrorResponse.Success;
            firstError = firstError?.TrimStart($"{targetPath} : ");
            return ErrorResponse.Fail(reason: firstError ?? "Unknown Error");
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

        private async Task CopyOverExtraData()
        {
            var inputExtraData = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(PathToProject), "Data"));
            if (!inputExtraData.Exists)
            {
                _output.OnNext("No extra data to consider.");
                return;
            }

            var outputExtraData = new DirectoryInfo(Path.Combine(PathToExtraDataBaseFolder, Name));
            if (outputExtraData.Exists)
            {
                _output.OnNext($"Extra data folder already exists. Leaving as is: {outputExtraData}");
                return;
            }

            _output.OnNext("Copying extra data folder");
            _output.OnNext($"  From: {inputExtraData}");
            _output.OnNext($"  To: {outputExtraData}");
            inputExtraData.DeepCopy(outputExtraData);
        }
    }
}
