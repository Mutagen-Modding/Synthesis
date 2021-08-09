using System.Diagnostics.CodeAnalysis;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public class CliPatcherSettings : PatcherSettings
    {
        public string PathToExecutable { get; set; } = string.Empty;

        public override void Print(ILogger logger)
        {
            logger.Information($"[CLI] {Nickname.Decorate(x => $"{x} => ")}{PathToExecutable}");
        }
    }
}
