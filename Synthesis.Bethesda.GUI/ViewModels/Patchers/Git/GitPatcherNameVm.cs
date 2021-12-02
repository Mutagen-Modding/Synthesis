using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Git
{
    public class GitPatcherNameVm : ViewModel, IPatcherNameVm
    {
        private readonly ObservableAsPropertyHelper<string> _name;
        public string Name => _name.Value;

        [Reactive] public string Nickname { get; set; } = string.Empty;

        public GitPatcherNameVm(
            IConstructName nameConstructor,
            IGitRemoteRepoPathInputVm remoteRepoPathFollower)
        {
            _name = remoteRepoPathFollower.WhenAnyValue(x => x.RemoteRepoPath)
                .Select(nameConstructor.Construct)
                .CombineLatest(
                    this.WhenAnyValue(x => x.Nickname),
                    (auto, nickname) => nickname.IsNullOrWhitespace() ? auto : nickname)
                .ToGuiProperty<string>(this, nameof(Name),
                    nameConstructor.Construct(remoteRepoPathFollower.RemoteRepoPath), deferSubscription: true);
        }
    }
}