namespace Synthesis.Bethesda.Execution.DotNet.Builder.Transient;

public interface IBuildOutputAccumulator
{
    string? FirstError { get; }
    IReadOnlyList<string> Output { get; }
    void Process(string line);
}

public class BuildOutputAccumulator : IBuildOutputAccumulator
{
    public const string BuildFailedString = "Build FAILED";
        
    private readonly List<string> _output = new();
    private int _totalLen = 0;
        
    public IReadOnlyList<string> Output => _output;
    public string? FirstError { get; private set; }
    public bool BuildFailed { get; private set; }
    public int Limit { get; set; } = 10_000;

    public BuildOutputAccumulator()
    {
        
    }

    public void Process(string line)
    {
        // ToDo
        // Refactor off looking for a string
        if (line.StartsWith(BuildFailedString))
        {
            BuildFailed = true;
        }
        else if (BuildFailed
                 && FirstError == null
                 && !string.IsNullOrWhiteSpace(line)
                 && line.StartsWith("error"))
        {
            FirstError = line;
        }
        if (_totalLen < Limit)
        {
            _totalLen += line.Length;
            _output.Add(line);
        }
    }
}