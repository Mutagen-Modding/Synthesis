using LibGit2Sharp;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class GithubPatcherInitVM : PatcherInitVM
    {
        private readonly GithubPatcherVM _patcher;
        public override PatcherVM Patcher => _patcher;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public GithubPatcherInitVM(GithubPatcherVM patcher)
        {
            _patcher = patcher;

            // Whenever we change, mark that we cannot
            _CanCompleteConfiguration = _patcher.WhenAnyValue(x => x.RepoPath)
                .DistinctUntilChanged()
                .Select(x => ErrorResponse.Fail("Checking remote repository correctness."))
                // But merge in the work of checking the repo on that same path to get the eventual result
                .Merge(_patcher.WhenAnyValue(x => x.RepoPath)
                    .DistinctUntilChanged()
                    .Debounce(TimeSpan.FromMilliseconds(300), RxApp.MainThreadScheduler)
                    .ObserveOn(RxApp.TaskpoolScheduler)
                    .Select(p =>
                    {
                        try
                        {
                            if (Repository.ListRemoteReferences(p).Any()) return ErrorResponse.Success;
                        }
                        catch (Exception)
                        {
                        }
                        return ErrorResponse.Fail("Path does not point to a valid repository.");
                    }))
                .Cast<ErrorResponse, ErrorResponse>()
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);
        }
    }
}
