using DynamicData.Kernel;
using LibGit2Sharp;
using Mutagen.Bethesda.Synthesis.Core.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class GithubPatcherVM : PatcherVM
    {
        [Reactive]
        public string RepoPath { get; set; } = string.Empty;

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        public override bool NeedsConfiguration => true;

        protected override IObservable<ErrorResponse> CanCompleteConfiguration =>
            // Whenever we change, mark that we cannot
            this.WhenAnyValue(x => x.RepoPath)
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
                            if (Repository.ListRemoteReferences(p).Any()) return ErrorResponse.Success;
                        }
                        catch (Exception)
                        {
                        }
                        return ErrorResponse.Fail("Path does not point to a valid repository.");
                    })
                    .ObserveOnGui());

        public GithubPatcherVM(MainVM mvm, GithubPatcherSettings? settings = null)
            : base(mvm, settings)
        {
            CopyInSettings(settings);

            _DisplayName = this.WhenAnyValue(
                x => x.Nickname,
                x => x.RepoPath,
                (nickname, path) =>
                {
                    if (!string.IsNullOrWhiteSpace(nickname)) return nickname;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(path)) return "Mutagen Github Link";
                        var span = path.AsSpan();
                        var slashIndex = span.LastIndexOf('/');
                        if (slashIndex != -1)
                        {
                            span = span.Slice(slashIndex + 1);
                        }
                        return span.ToString();
                    }
                    catch (Exception)
                    {
                        return "Mutagen Github Link";
                    }
                })
                .ToGuiProperty<string>(this, nameof(DisplayName));
        }

        public override PatcherSettings Save()
        {
            var ret = new GithubPatcherSettings();
            CopyOverSave(ret);
            ret.RepoPath = this.RepoPath;
            return ret;
        }

        private void CopyInSettings(GithubPatcherSettings? settings)
        {
            if (settings == null) return;
            this.RepoPath = settings.RepoPath;
        }
    }
}
