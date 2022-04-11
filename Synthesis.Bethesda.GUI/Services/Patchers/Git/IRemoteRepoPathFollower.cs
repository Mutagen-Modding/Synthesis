using System;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IRemoteRepoPathFollower
{
    public IObservable<string> Path { get; }
}