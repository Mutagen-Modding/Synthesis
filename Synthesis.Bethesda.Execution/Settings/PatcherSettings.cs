using System.Diagnostics.CodeAnalysis;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.Execution.Settings;

[ExcludeFromCodeCoverage]
public abstract class PatcherSettings : IPatcherNicknameProvider
{
    public bool On { get; set; } 
    public string Nickname { get; set; } = string.Empty;

    public abstract void Print(ILogger logger);
}