using System.Collections.Generic;

namespace Synthesis.Bethesda.Execution.Utility
{
    public record ProcessRunReturn(int Result, List<string> Out, List<string> Errors)
    {
        public ProcessRunReturn()
            : this(-1, new(), new())
        {
        }
    }
}