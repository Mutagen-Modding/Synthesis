using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Utility;

[ExcludeFromCodeCoverage]
public record ProcessRunReturn(int Result, List<string> Out, List<string> Errors)
{
    public ProcessRunReturn()
        : this(-1, new(), new())
    {
    }
}