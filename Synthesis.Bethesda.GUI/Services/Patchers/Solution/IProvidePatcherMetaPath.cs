using System;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution
{
    public interface IProvidePatcherMetaPath
    {
        IObservable<string> Path { get; }
    }
}