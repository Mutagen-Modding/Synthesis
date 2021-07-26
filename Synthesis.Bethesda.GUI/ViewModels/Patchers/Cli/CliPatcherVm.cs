using System.Reactive.Linq;
using Autofac;
using Noggog;
using Noggog.Utility;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Patchers.Cli;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli
{
    public class CliPatcherVm : PatcherVm
    {
        public IPathToExecutableInputVm ExecutableInput { get; }
        private readonly ILifetimeScope _Scope;
        private readonly IPatcherExtraDataPathProvider _ExtraDataPathProvider;
        private readonly IProcessFactory _ProcessFactory;
        public IShowHelpSetting ShowHelpSetting { get; }

        private readonly ObservableAsPropertyHelper<ConfigurationState> _State;
        public override ConfigurationState State => _State?.Value ?? ConfigurationState.Success;

        public CliPatcherVm(
            IPatcherNameVm nameVm,
            IPathToExecutableInputVm pathToExecutableInputVm,
            IRemovePatcherFromProfile remove,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            IShowHelpSetting showHelpSetting,
            ILifetimeScope scope,
            IPatcherExtraDataPathProvider extraDataPathProvider,
            IProcessFactory processFactory,
            CliPatcherSettings? settings = null)
            : base(scope, nameVm, remove, selPatcher, confirmation, settings)
        {
            ExecutableInput = pathToExecutableInputVm;
            _Scope = scope;
            _ExtraDataPathProvider = extraDataPathProvider;
            _ProcessFactory = processFactory;
            ShowHelpSetting = showHelpSetting;
            CopyInSettings(settings);

            _State = pathToExecutableInputVm.WhenAnyValue(x => x.Picker.ErrorState)
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
            ExecutableInput.Picker.TargetPath = settings.PathToExecutable;
        }

        public override PatcherSettings Save()
        {
            var ret = new CliPatcherSettings();
            CopyOverSave(ret);
            ret.PathToExecutable = ExecutableInput.Picker.TargetPath;
            return ret;
        }
    }
}
