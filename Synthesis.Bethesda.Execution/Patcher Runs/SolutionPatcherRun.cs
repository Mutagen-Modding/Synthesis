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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public class SolutionPatcherRun : IPatcherRun
    {
        public string Name { get; }
        public string PathToSolution { get; }
        public string PathToProject { get; }
        public string PathToExe { get; }
        public CliPatcherRun? CliRun { get; private set; }

        private Subject<string> _output = new Subject<string>();
        public IObservable<string> Output => _output;

        private Subject<string> _error = new Subject<string>();
        public IObservable<string> Error => _error;

        public SolutionPatcherRun(string nickname, string pathToSln, string pathToProj, string pathToExe)
        {
            PathToSolution = pathToSln;
            PathToProject = pathToProj;
            PathToExe = pathToExe;
            Name = $"{Path.GetFileNameWithoutExtension(pathToSln)} => {Path.GetFileNameWithoutExtension(pathToProj)}";
        }

        public async Task Prep(GameRelease release, CancellationToken? cancel = null)
        {
            CliRun = new CliPatcherRun(nickname: Name, pathToExecutable: PathToExe);

            var resp = await CompileWithDotnet(PathToSolution, cancel ?? CancellationToken.None).ConfigureAwait(false);
            if (!resp.Succeeded)
            {
                throw new SynthesisBuildFailure(resp.Reason);
            }
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
        {
            if (CliRun == null)
            {
                throw new SynthesisBuildFailure("Expected CLI Run object did not exist.");
            }
            using var outputSub = CliRun.Output.Subscribe(_output);
            using var errSub = CliRun.Error.Subscribe(_error);
            await CliRun.Run(settings, cancel).ConfigureAwait(false);
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

        public static async Task<ErrorResponse> CompileWithDotnet(string solutionUrl, CancellationToken cancel)
        {
            using var process = ProcessWrapper.Start(
                new ProcessStartInfo("dotnet", $"build \"{solutionUrl}\""),
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
    }
}
