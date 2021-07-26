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
using Serilog;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.Execution.Patchers.Running
{
    public interface ISolutionPatcherRun : IPatcherRun
    {
    }

    public class SolutionPatcherRun : ISolutionPatcherRun
    {
        private readonly CompositeDisposable _disposable = new();

        private readonly ICopyOverExtraData _copyOverExtraData;
        private readonly IPathToSolutionFileProvider _pathToSln;
        private readonly IPatcherExtraDataPathProvider _patcherExtraDataPathProvider;
        private readonly IPatcherNameProvider _nameProvider;
        private readonly IPathToProjProvider _pathToProjProvider;
        private readonly IBuild _build;
        private readonly ILogger _logger;
        private readonly ICheckRunnability _CheckRunnability;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IDotNetCommandStartConstructor _commandStartConstructor;
        private readonly IDefaultDataPathProvider _defaultDataPathProvider;
        private readonly IProvideRepositoryCheckouts _RepositoryCheckouts;
        public string Name => _nameProvider.Name;

        public SolutionPatcherRun(
            IPathToSolutionFileProvider pathToSln, 
            IPatcherExtraDataPathProvider patcherExtraDataPathProvider,
            IPatcherNameProvider nameProvider,
            ICopyOverExtraData copyOverExtraData,
            IPathToProjProvider pathToProjProvider,
            IBuild build,
            ILogger logger,
            ICheckRunnability checkRunnability,
            IProcessFactory processFactory,
            IDotNetCommandStartConstructor commandStartConstructor,
            IDefaultDataPathProvider defaultDataPathProvider,
            IProvideRepositoryCheckouts repositoryCheckouts)
        {
            _pathToSln = pathToSln;
            _patcherExtraDataPathProvider = patcherExtraDataPathProvider;
            _nameProvider = nameProvider;
            _pathToProjProvider = pathToProjProvider;
            _copyOverExtraData = copyOverExtraData;
            _build = build;
            _logger = logger;
            _CheckRunnability = checkRunnability;
            _ProcessFactory = processFactory;
            _commandStartConstructor = commandStartConstructor;
            _defaultDataPathProvider = defaultDataPathProvider;
            _RepositoryCheckouts = repositoryCheckouts;
        }

        public async Task Prep(GameRelease release, CancellationToken cancel)
        {
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    _logger.Information("Compiling");
                    var resp = await _build.Compile(_pathToProjProvider.Path, cancel).ConfigureAwait(false);
                    if (!resp.Succeeded)
                    {
                        throw new SynthesisBuildFailure(resp.Reason);
                    }
                    _logger.Information("Compiled");
                }),
                Task.Run(() =>
                {
                    _copyOverExtraData.Copy();
                })).ConfigureAwait(false); ;
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
        {
            var repoPath = Path.GetDirectoryName(_pathToSln.Path);
            if (Repository.IsValid(repoPath))
            {
                using var repo = _RepositoryCheckouts.Get(repoPath!);
                _logger.Information("Sha {Sha}", repo.Repository.CurrentSha);
            }
            
            var runnability = await _CheckRunnability.Check(
                _pathToProjProvider.Path,
                directExe: false,
                release: settings.GameRelease,
                dataFolder: settings.DataFolderPath,
                loadOrderPath: settings.LoadOrderFilePath,
                cancel: cancel);

            if (runnability.Failed)
            {
                throw new CliUnsuccessfulRunException((int)Codes.NotRunnable, runnability.Reason);
            }

            var defaultDataFolderPath = _defaultDataPathProvider.Path;

            var internalSettings = new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = settings.DataFolderPath,
                ExtraDataFolder = _patcherExtraDataPathProvider.Path,
                DefaultDataFolderPath = Directory.Exists(defaultDataFolderPath) ? defaultDataFolderPath.Path : null,
                GameRelease = settings.GameRelease,
                LoadOrderFilePath = settings.LoadOrderFilePath,
                OutputPath = settings.OutputPath,
                SourcePath = settings.SourcePath,
                PatcherName = settings.PatcherName,
                PersistencePath = settings.PersistencePath
            };
            var args = Parser.Default.FormatCommandLine(internalSettings);
            using var process = _ProcessFactory.Create(
                _commandStartConstructor.Construct("run --project", _pathToProjProvider.Path, "--no-build", args),
                cancel: cancel);
            _logger.Information("Running");
            _logger.Information("({WorkingDirectory}): {FileName} {Args}",
                process.StartInfo.WorkingDirectory,
                process.StartInfo.FileName,
                process.StartInfo.Arguments);
            using var outputSub = process.Output.Subscribe(x => _logger.Information(x));
            using var errSub = process.Error.Subscribe(x => _logger.Error(x));
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

        public override string ToString()
        {
            return Name;
        }
    }
}
