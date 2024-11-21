namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.Instantiation;

public class GitIdAllocator
{
    public string GetNewId(IReadOnlyCollection<string> existingIds)
    {
        for (int i = 0; i < 15; i++)
        {
            var attempt = Path.GetRandomFileName();
            if (!existingIds.Contains(attempt))
            {
                return attempt;
            }
        }

        throw new ArgumentException("Could not allocate a new profile");
    }
}