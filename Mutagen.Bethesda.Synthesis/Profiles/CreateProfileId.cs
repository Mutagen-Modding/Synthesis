namespace Mutagen.Bethesda.Synthesis.Profiles;

public class CreateProfileId
{
    public string GetNewProfileId(IReadOnlyCollection<string> existing)
    {
        for (int i = 0; i < 15; i++)
        {
            var attempt = Path.GetRandomFileName();
            if (!existing.Contains(attempt))
            {
                return attempt;
            }
        }

        throw new ArgumentException("Could not allocate a new profile");
    }
}