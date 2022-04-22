namespace Synthesis.Bethesda.Execution.Patchers.Git;

public interface IConstructName
{
    string Construct(string path);
}

public class ConstructName : IConstructName
{
    public const string FallbackName = "Mutagen Git Patcher";
        
    public string Construct(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return "Mutagen Git Patcher";
            var span = path.AsSpan();
            var slashIndex = span.LastIndexOf('/');
            if (slashIndex != -1)
            {
                span = span.Slice(slashIndex + 1);
            }
            return span.ToString();
        }
        catch (Exception)
        {
            return "Mutagen Git Patcher";
        }
    }
}