using System;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Git
{
    public interface IGitRemoteRepoPathInputVm
    {
        string RemoteRepoPath { get; set; }
    }

    public class GitRemoteRepoPathInputVm : ViewModel, IRemoteRepoPathProvider, IGitRemoteRepoPathInputVm
    {
        [Reactive] public string RemoteRepoPath { get; set; } = string.Empty;
        IObservable<string> IRemoteRepoPathProvider.Path => this.WhenAnyValue(x => x.RemoteRepoPath);
    }
}