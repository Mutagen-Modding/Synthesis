using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
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
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;

namespace Synthesis.Bethesda.Execution.Patchers.Solution
{
    public interface ISolutionPatcherRun : IPatcherRun
    {
    }

    public class SolutionPatcherRun : ISolutionPatcherRun
    {
        private readonly CompositeDisposable _disposable = new();

        private readonly IBuild _Build;
        private readonly ICheckRunnability _CheckRunnability;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideRepositoryCheckouts _RepositoryCheckouts;
        public string Name { get; }
        public string PathToSolution { get; }
        public string PathToProject { get; }
        public string PathToExtraDataBaseFolder { get; }

        private readonly Subject<string> _output = new();
        public IObservable<string> Output => _output;

        private readonly Subject<string> _error = new();
        public IObservable<string> Error => _error;

        public delegate ISolutionPatcherRun Factory(
            string name,
            string pathToSln,
            string pathToProj,
            string pathToExtraDataBaseFolder);

        public SolutionPatcherRun(
            string pathToSln, 
            string pathToProj,
            string pathToExtraDataBaseFolder,
            string name,
            IBuild build,
            ICheckRunnability checkRunnability,
            IProcessFactory processFactory,
            IProvideRepositoryCheckouts repositoryCheckouts)
        {
            Name = name;
            _Build = build;
            _CheckRunnability = checkRunnability;
            _ProcessFactory = processFactory;
            _RepositoryCheckouts = repositoryCheckouts;
            PathToSolution = pathToSln;
            PathToProject = pathToProj;
            PathToExtraDataBaseFolder = pathToExtraDataBaseFolder;
        }

        public async Task Prep(GameRelease release, CancellationToken cancel)
        {
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    _output.OnNext($"Compiling");
                    var resp = await _Build.Compile(PathToProject, cancel, _output.OnNext).ConfigureAwait(false);
                    if (!resp.Succeeded)
                    {
                        throw new SynthesisBuildFailure(resp.Reason);
                    }
                    _output.OnNext($"Compiled");
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
                using var repo = _RepositoryCheckouts.Get(repoPath!);
                _output.OnNext($"Sha {repo.Repository.CurrentSha}");
            }
            
            var runnability = await _CheckRunnability.Check(
                PathToProject,
                directExe: false,
                release: settings.GameRelease,
                dataFolder: settings.DataFolderPath,
                loadOrderPath: settings.LoadOrderFilePath,
                cancel: cancel,
                log: (s) => _output.OnNext(s));

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
            using var process = _ProcessFactory.Create(
                new ProcessStartInfo("dotnet", $"run --project \"{PathToProject}\" --runtime win-x64 --no-build -c Release {args}"),
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

        public void AddForDisposal(IDisposable disposable)
        {
            _disposable.Add(disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        // Almost there, I think, but not currently working.
        public static async Task<(bool OverallSuccess, EmitResult? TriggeringFailure)> CompileWithRosyln(string solutionUrl, string outputDir, CancellationToken cancel)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Microsoft.CodeAnalysis.Solution solution = workspace.OpenSolutionAsync(solutionUrl).Result;
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
