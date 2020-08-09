using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    public class CliUnsuccessfulRunException : Exception
    {
        public int ExitCode { get; }

        public CliUnsuccessfulRunException(int exitCode, string message) 
            : base(message)
        {
            ExitCode = exitCode;
        }
    }
}
