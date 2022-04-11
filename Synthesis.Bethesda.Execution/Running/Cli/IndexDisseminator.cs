namespace Synthesis.Bethesda.Execution.Running.Cli;

public interface IIndexDisseminator
{
    int GetNext();
}

public class IndexDisseminator : IIndexDisseminator
{
    private int _index = 1;

    public int GetNext() => _index++;
}