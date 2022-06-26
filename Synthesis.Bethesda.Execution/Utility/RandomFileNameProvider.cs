using Noggog;

namespace Synthesis.Bethesda.Execution.Utility;

public interface IRandomFileNameProvider
{
    FileName Get();
}

public class RandomFileNameProvider : IRandomFileNameProvider
{
    public FileName Get()
    {
        return Path.GetRandomFileName();
    }
}