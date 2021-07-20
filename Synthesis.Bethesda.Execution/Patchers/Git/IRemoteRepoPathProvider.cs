using System;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IRemoteRepoPathProvider
    {
        public IObservable<string> Path { get; }
    }
}