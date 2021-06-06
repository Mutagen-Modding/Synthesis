using System;

namespace Synthesis.Bethesda.Execution.Versioning
{
    public interface IConsiderPrereleasePreference
    {
        IObservable<bool> ConsiderPrereleases { get; }
    }
}