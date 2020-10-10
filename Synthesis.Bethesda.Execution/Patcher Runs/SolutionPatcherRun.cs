using Buildalyzer;
using Buildalyzer.Environment;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using Mutagen.Bethesda;
using Noggog;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.Common;

namespace Synthesis.Bethesda.Execution
{
    public class SolutionPatcherRun : IPatcherRun
    {
        public string Name { get; }
        public string PathToSolution { get; }
        public string PathToProject { get; }
        public string? PathToExe { get; }
        public CliPatcherRun? CliRun { get; private set; }

        private Subject<string> _output = new Subject<string>();
        public IObservable<string> Output => _output;

        private Subject<string> _error = new Subject<string>();
        public IObservable<string> Error => _error;

        public SolutionPatcherRun(
            string nickname,
            string pathToSln, 
            string pathToProj,
            string? pathToExe)
        {
            PathToSolution = pathToSln;
            PathToProject = pathToProj;
            PathToExe = pathToExe;
            Name = $"{nickname} => {Path.GetFileNameWithoutExtension(pathToSln)}/{Path.GetFileNameWithoutExtension(pathToProj)}";
        }

        public async Task Prep(GameRelease release, ILogger? log, CancellationToken? cancel = null)
        {
            var pathToExe = PathToExe;
            if (pathToExe == null)
            {
                log?.Write($"Locating path to exe based on proj path {PathToProject}");
                var pathToExeGet = await GetPathToExe(PathToProject, cancel ?? CancellationToken.None);
                if (pathToExeGet.Failed)
                {
                    throw pathToExeGet.Exception ?? throw new ArgumentException("Could not find path to exe");
                }
                pathToExe = pathToExeGet.Value;
                log?.Write($"Located path to exe: {pathToExe}");
            }

            CliRun = new CliPatcherRun(
                nickname: Name,
                pathToExecutable: pathToExe,
                pathToExtra: Path.Combine(Path.GetDirectoryName(PathToProject), "Data"));

            log?.Write($"Compiling");
            var resp = await CompileWithDotnet(PathToProject, cancel ?? CancellationToken.None).ConfigureAwait(false);
            if (!resp.Succeeded)
            {
                throw new SynthesisBuildFailure(resp.Reason);
            }
            log?.Write($"Compiled");
        }

        public async Task Run(RunSynthesisPatcher settings, ILogger? log, CancellationToken? cancel = null)
        {
            if (CliRun == null)
            {
                throw new SynthesisBuildFailure("Expected CLI Run object did not exist.");
            }
            using var outputSub = CliRun.Output.Subscribe(_output);
            using var errSub = CliRun.Error.Subscribe(_error);
            await CliRun.Run(settings, log, cancel).ConfigureAwait(false);
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

        public static async Task<ErrorResponse> CompileWithDotnet(string url, CancellationToken cancel)
        {
            using var process = ProcessWrapper.Start(
                new ProcessStartInfo("dotnet", $"build \"{url}\""),
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
            return ErrorResponse.Fail(reason: firstError ?? "Unknown Error");
        }

        public static async Task<GetResponse<string>> GetPathToExe(string projectPath, CancellationToken cancel)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();
                if (!File.Exists(projectPath))
                {
                    return GetResponse<string>.Fail("Project path does not exist.");
                }

                // Right now this is slow as it cleans the build results unnecessarily.  Need to look into that
                var manager = new AnalyzerManager();
                cancel.ThrowIfCancellationRequested();
                var proj = manager.GetProject(projectPath);
                cancel.ThrowIfCancellationRequested();
                var opt = new EnvironmentOptions();
                opt.TargetsToBuild.SetTo("Build");
                var build = proj.Build();
                cancel.ThrowIfCancellationRequested();
                var results = build.Results.ToArray();
                if (results.Length != 1)
                {
                    return GetResponse<string>.Fail("Unsupported number of build results.");
                }
                var result = results[0];
                if (!result.Properties.TryGetValue("RunCommand", out var cmd))
                {
                    return GetResponse<string>.Fail("Could not find executable to be run");
                }

                return GetResponse<string>.Succeed(cmd);
            }
            catch (Exception ex)
            {
                return GetResponse<string>.Fail(ex);
            }
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
    }
}
