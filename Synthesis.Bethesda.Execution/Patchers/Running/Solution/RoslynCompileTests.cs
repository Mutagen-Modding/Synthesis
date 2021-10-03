using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution
{
    [ExcludeFromCodeCoverage]
    public class RoslynCompileTests
    {
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
    }
}