using Noggog;
using System.Diagnostics.CodeAnalysis;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public class SolutionPatcherSettings : PatcherSettings, 
        IPathToSolutionFileProvider, 
        IProjectSubpathDefaultSettings,
        IPatcherNameProvider
    {
        public FilePath SolutionPath { get; set; } = string.Empty;
        public string ProjectSubpath { get; set; } = string.Empty;

        public override void Print(ILogger logger)
        {
            logger.Information($"[Solution] {Nickname.Decorate(x => $"{x} => ")}{SolutionPath} => {ProjectSubpath}");
        }

        FilePath IPathToSolutionFileProvider.Path => SolutionPath;
        string IPatcherNameProvider.Name => Nickname;
    }
}
