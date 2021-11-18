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
        private readonly IConstructName _nameConstructor;
        private readonly IGitRemoteRepoPathInputVm _remoteRepoPathFollower;
        
        private readonly ObservableAsPropertyHelper<string> _Name;
        public string Name => _Name.Value;

        [Reactive] public string Nickname { get; set; } = string.Empty;

        public GitPatcherNameVm(
            IConstructName nameConstructor,
            IGitRemoteRepoPathInputVm remoteRepoPathFollower)
        {
            _nameConstructor = nameConstructor;
            _remoteRepoPathFollower = remoteRepoPathFollower;
            _Name = _remoteRepoPathFollower.WhenAnyValue(x => x.RemoteRepoPath)
                .Select(_nameConstructor.Construct)
                .CombineLatest(
                    this.WhenAnyValue(x => x.Nickname),
                    (auto, nickname) => nickname.IsNullOrWhitespace() ? auto : nickname)
                .ToGuiProperty<string>(this, nameof(Name),
                    _nameConstructor.Construct(_remoteRepoPathFollower.RemoteRepoPath));
        }
    }
}