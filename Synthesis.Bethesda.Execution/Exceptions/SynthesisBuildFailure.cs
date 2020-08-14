using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    public class SynthesisBuildFailure : Exception
    {
        public SynthesisBuildFailure(string firstError)
            : base(firstError)
        {
        }
    }
}
