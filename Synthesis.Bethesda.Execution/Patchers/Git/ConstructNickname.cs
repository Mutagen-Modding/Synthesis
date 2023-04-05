namespace Synthesis.Bethesda.Execution.Patchers.Git;

public interface IConstructNameFromRepositoryPath
{
    string Construct(string path);
}

public class ConstructNameFromRepositoryPath : IConstructNameFromRepositoryPath
{
    public const string FallbackName = "Mutagen Git Patcher";
        
    public string Construct(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path)) return "Mutagen Git Patcher";
            var span = path.AsSpan();
            var slashIndex = span.LastIndexOf('/');
            while (slashIndex != -1)
            {
                var name = span.Slice(slashIndex + 1);
                if (name.IsWhiteSpace())
                {
                    span = span.Slice(0, slashIndex);
                    slashIndex = span.LastIndexOf('/');
                }
                else
                {
                    return name.ToString();
                }
            }

            if (!span.IsWhiteSpace())
            {
                return span.ToString();
            }
        }
        catch (Exception)
        {
        }
        
        return "Mutagen Git Patcher";
    }
}