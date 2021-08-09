using Noggog;
using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public class SolutionPatcherSettings : PatcherSettings
    {
        public string SolutionPath = string.Empty;
        public string ProjectSubpath = string.Empty;

        public override void Print(ILogger logger)
        {
            logger.Information($"[Solution] {Nickname.Decorate(x => $"{x} => ")}{SolutionPath} => {ProjectSubpath}");
        }
    }
}
