using LibGit2Sharp;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI
{
    public class GithubPatcherInitVM : PatcherInitVM
    {
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public GithubPatcherVM Patcher { get; }

        public GithubPatcherInitVM(ProfileVM profile)
            : base(profile)
        {
            Patcher = new GithubPatcherVM(profile);
            this.CompositeDisposable.Add(Patcher);

            _CanCompleteConfiguration = this.WhenAnyValue(x => x.Patcher.State)
                .Select(x => x.RunnableState)
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);
        }

        public override async IAsyncEnumerable<PatcherVM> Construct()
        {
            yield return Patcher;
        }

        public override void Cancel()
        {
            base.Cancel();
            Patcher.Delete();
        }
    }
}
