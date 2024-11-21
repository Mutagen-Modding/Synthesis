using System.Reactive;
using System.Reactive.Linq;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

namespace Synthesis.Bethesda.GUI.Services.Profile.TopLevel;

public class AddGitPatcherResponder
{
    private readonly IPatcherFactory _patcherFactory;
    private readonly IAddPatchersToSelectedGroupVm _addPatchersToSelectedGroupVm;
    private readonly IMetaFileReceiver _metaFileReceiver;
    private readonly PatcherInitRenameValidator _renamer;

    public AddGitPatcherResponder(
        IPatcherFactory patcherFactory,
        IAddPatchersToSelectedGroupVm addPatchersToSelectedGroupVm,
        IMetaFileReceiver metaFileReceiver,
        PatcherInitRenameValidator renamer)
    {
        _patcherFactory = patcherFactory;
        _addPatchersToSelectedGroupVm = addPatchersToSelectedGroupVm;
        _metaFileReceiver = metaFileReceiver;
        _renamer = renamer;
    }
    
    public IObservable<Unit> Connect()
    {
        return _metaFileReceiver.MetaFiles
            .Select(x => x.AddGitPatcher)
            .WhereNotNull()
            .DoTask(async x =>
            {
                if (!_addPatchersToSelectedGroupVm.CanAddPatchers) return;
                var gitPatcher = _patcherFactory.GetGitPatcher(new GithubPatcherSettings()
                {
                    RemoteRepoPath = x.Url,
                    SelectedProjectSubpath = x.SelectedProject
                });
                if (!await _renamer.ConfirmNameUnique(gitPatcher)) return;
                _addPatchersToSelectedGroupVm.AddNewPatchers(gitPatcher);
            })
            .Unit();
    }
}