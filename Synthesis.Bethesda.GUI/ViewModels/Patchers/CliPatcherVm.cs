using System;
using System.IO;
using System.Reactive.Linq;
using Noggog;
using Noggog.Utility;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers
{
    public class CliPatcherVm : PatcherVm
    {
        private readonly IProcessFactory _ProcessFactory;
        public IShowHelpSetting ShowHelpSetting { get; }
        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        public readonly PathPickerVM PathToExecutable = new()
        {
             PathType = PathPickerVM.PathTypeOptions.File,
             ExistCheckOption = PathPickerVM.CheckOptions.On,
        };

        private readonly ObservableAsPropertyHelper<ConfigurationState> _State;
        public override ConfigurationState State => _State?.Value ?? ConfigurationState.Success;

        public delegate CliPatcherVm Factory(CliPatcherSettings settings);

        public CliPatcherVm(
            IRemovePatcherFromProfile remove,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            IShowHelpSetting showHelpSetting,
            IProcessFactory processFactory,
            CliPatcherSettings? settings = null)
            : base(remove, selPatcher, confirmation, settings)
        {
            _ProcessFactory = processFactory;
            ShowHelpSetting = showHelpSetting;
            CopyInSettings(settings);
            _DisplayName = this.WhenAnyValue(
                    x => x.Nickname,
                    x => x.PathToExecutable.TargetPath,
                    (Nickname, PathToExecutable) => (Nickname, PathToExecutable))
                .Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x.Nickname))
                    {
                        try
                        {
                            return Path.GetFileNameWithoutExtension(x.PathToExecutable);
                        }
                        catch (Exception)
                        {
                            return "<Naming Error>";
                        }
                    }
                    else
                    {
                        return x.Nickname;
                    }
                })
                .ToGuiProperty<string>(this, nameof(DisplayName), string.Empty);

            _State = this.WhenAnyValue(x => x.PathToExecutable.ErrorState)
                .Select(e =>
                {
                    return new ConfigurationState()
                    {
                        IsHaltingError = !e.Succeeded,
                        RunnableState = e
                    };
                })
                .ToGuiProperty<ConfigurationState>(this, nameof(State), new ConfigurationState(ErrorResponse.Fail("Evaluating"))
                {
                    IsHaltingError = false
                });
        }
        
        private void CopyInSettings(CliPatcherSettings? settings)
        {
            if (settings == null) return;
            PathToExecutable.TargetPath = settings.PathToExecutable;
        }

        public override PatcherSettings Save()
        {
            var ret = new CliPatcherSettings();
            CopyOverSave(ret);
            ret.PathToExecutable = PathToExecutable.TargetPath;
            return ret;
        }

        public override PatcherRunVm ToRunner(PatchersRunVm parent)
        {
            return new PatcherRunVm(
                parent, 
                this, 
                new CliPatcherRun(
                    _ProcessFactory,
                    nickname: DisplayName, 
                    pathToExecutable: PathToExecutable.TargetPath, 
                    pathToExtra: null));
        }
    }
}
