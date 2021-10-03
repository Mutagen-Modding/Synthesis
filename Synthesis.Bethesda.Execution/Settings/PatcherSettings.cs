using System.Diagnostics.CodeAnalysis;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public abstract class PatcherSettings
    {
        public bool On;
        public string Nickname = string.Empty;

        public abstract void Print(ILogger logger);
    }
}
