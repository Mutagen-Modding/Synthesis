using System;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget
{
    public class CorruptError : NotExistsError
    {
        public override string ErrorText => $"Config was corrupt.  Can fix by replacing the whole file.";
        
        public Exception Exception { get; }
        
        public CorruptError(Exception ex)
        {
            Exception = ex;
        }
    }
}