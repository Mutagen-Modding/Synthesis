namespace Synthesis.Bethesda.Execution.Exceptions;

public class ClassifiedErrorException : Exception
{
    public ClassifiedErrorException()
        : base("Error was classified and handled")
    {
    }

    public ClassifiedErrorException(Exception? innerException)
        : base("Error was classified and handled", innerException)
    {
    }
}