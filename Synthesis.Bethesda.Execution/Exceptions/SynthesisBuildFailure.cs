using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution;

[ExcludeFromCodeCoverage]
public class SynthesisBuildFailure : Exception
{
    public SynthesisBuildFailure(string firstError)
        : base(firstError)
    {
    }
}