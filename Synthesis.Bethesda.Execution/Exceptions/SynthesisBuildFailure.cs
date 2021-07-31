using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    [ExcludeFromCodeCoverage]
    public class SynthesisBuildFailure : Exception
    {
        public SynthesisBuildFailure(string firstError)
            : base(firstError)
        {
        }
    }
}
