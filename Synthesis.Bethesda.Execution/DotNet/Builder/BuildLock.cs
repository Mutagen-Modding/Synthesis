using System.Collections.Concurrent;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution.DotNet.Builder;

public interface IBuildLock
{
    AsyncLock GetLock(string path);
}

public class BuildLock : IBuildLock
{
    private readonly ConcurrentDictionary<string, AsyncLock> _locks = new(StringComparer.OrdinalIgnoreCase);

    public AsyncLock GetLock(string path)
    {
        var normalized = Path.GetFullPath(path);
        return _locks.GetOrAdd(normalized, _ => new AsyncLock());
    }
}
