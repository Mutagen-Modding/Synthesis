﻿using System;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.Services.Patchers.Git;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Git
{
    public interface IGitRemoteRepoPathInputVm
    {
        string RemoteRepoPath { get; set; }
    }

    public class GitRemoteRepoPathInputVm : ViewModel, IRemoteRepoPathFollower, IGitRemoteRepoPathInputVm
    {
        [Reactive] public string RemoteRepoPath { get; set; } = string.Empty;
        IObservable<string> IRemoteRepoPathFollower.Path => this.WhenAnyValue(x => x.RemoteRepoPath);
    }
}