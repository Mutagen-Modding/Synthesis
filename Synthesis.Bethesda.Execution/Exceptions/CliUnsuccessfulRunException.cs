using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution;

[ExcludeFromCodeCoverage]
public class CliUnsuccessfulRunException : Exception
{
    public int ExitCode { get; }

    public CliUnsuccessfulRunException(int exitCode, string message) 
        : base(message)
    {
        ExitCode = exitCode;
    }
}