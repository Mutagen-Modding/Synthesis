using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI
{
    public class GithubPatcherInitVM : PatcherInitVM
    {
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        [Reactive]
        public string RepoPath { get; set; } = string.Empty;

        public GithubPatcherInitVM(ProfileVM profile)
            : base(profile)
        {
            // Whenever we change, mark that we cannot
            _CanCompleteConfiguration = this.WhenAnyValue(x => x.RepoPath)
                .DistinctUntilChanged()
                .Select(x => ErrorResponse.Fail("Checking remote repository correctness."))
                // But merge in the work of checking the repo on that same path to get the eventual result
                .Merge(this.WhenAnyValue(x => x.RepoPath)
                    .DistinctUntilChanged()
                    .Debounce(TimeSpan.FromMilliseconds(300), RxApp.MainThreadScheduler)
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(p =>
                    {
                        try
                        {
                            //if (Repository.ListRemoteReferences(p).Any()) return ErrorResponse.Success;
                        }
                        catch (Exception)
                        {
                        }
                        return ErrorResponse.Fail("Path does not point to a valid repository.");
                    }))
                .Cast<ErrorResponse, ErrorResponse>()
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);
        }

        public override async IAsyncEnumerable<PatcherVM> Construct()
        {
            yield return new GithubPatcherVM(Profile)
            {
                RepoPath = this.RepoPath
            };
        }
    }
}
