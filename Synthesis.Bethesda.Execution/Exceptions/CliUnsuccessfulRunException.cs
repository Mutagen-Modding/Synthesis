using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
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
}
