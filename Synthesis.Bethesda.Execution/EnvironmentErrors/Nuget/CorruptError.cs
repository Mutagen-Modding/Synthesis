using System.IO.Abstractions;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;

public class CorruptError : NotExistsError
{
    public override string ErrorText => $"Config was corrupt.  Can fix by replacing the whole file.";
        
    public Exception Exception { get; }
        
    public CorruptError(
        IFileSystem fileSystem,
        Exception ex)
        : base(fileSystem)
    {
        Exception = ex;
    }
}