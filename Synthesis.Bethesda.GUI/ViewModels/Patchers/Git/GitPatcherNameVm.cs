using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
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

        public GitPatcherNameVm(
            IConstructName nameConstructor,
            IGitRemoteRepoPathInputVm remoteRepoPathFollower)
        {
            _nameConstructor = nameConstructor;
            _remoteRepoPathFollower = remoteRepoPathFollower;
            _Name = _remoteRepoPathFollower.WhenAnyValue(x => x.RemoteRepoPath)
                .Select(_nameConstructor.Construct)
                .ToGuiProperty<string>(this, nameof(Name),
                    _nameConstructor.Construct(_remoteRepoPathFollower.RemoteRepoPath));
        }
    }
}