namespace Synthesis.Bethesda.Execution.Patchers.Running;

public interface IIndexDisseminator
{
    int GetNext();
}

public class IndexDisseminator : IIndexDisseminator
{
    private int _index = 1;

    public int GetNext() => _index++;
}