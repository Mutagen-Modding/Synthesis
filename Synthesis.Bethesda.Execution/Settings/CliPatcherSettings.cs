using System.Diagnostics.CodeAnalysis;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Cli;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public class CliPatcherSettings : PatcherSettings, IPathToExecutableInputProvider
    {
        public string PathToExecutable { get; set; } = string.Empty;

        public override void Print(ILogger logger)
        {
            logger.Information($"[CLI] {Nickname.Decorate(x => $"{x} => ")}{PathToExecutable}");
        }

        FilePath IPathToExecutableInputProvider.Path => PathToExecutable;
    }
}
