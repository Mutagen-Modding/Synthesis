using System.Diagnostics.CodeAnalysis;
using Noggog;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public class CliPatcherSettings : PatcherSettings
    {
        public string PathToExecutable { get; set; } = string.Empty;

        public override void Print(IRunReporter logger)
        {
            logger.Write(default(int), default, $"[CLI] {Nickname.Decorate(x => $"{x} => ")}{PathToExecutable}");
        }
    }
}
