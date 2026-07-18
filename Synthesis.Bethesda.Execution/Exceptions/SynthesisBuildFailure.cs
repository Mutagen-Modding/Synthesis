using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Exceptions;

[ExcludeFromCodeCoverage]
public class SynthesisBuildFailure : Exception
{
    public SynthesisBuildFailure(string firstError)
        : base(firstError)
    {
    }
}