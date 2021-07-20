using System;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Git
{
    public interface IGitInputVm
    {
        string RemoteRepoPath { get; set; }
    }

    public class GitInputVm : ViewModel, IRemoteRepoPathProvider, IGitInputVm
    {
        [Reactive] public string RemoteRepoPath { get; set; } = string.Empty;
        IObservable<string> IRemoteRepoPathProvider.Path => this.WhenAnyValue(x => x.RemoteRepoPath);
    }
}