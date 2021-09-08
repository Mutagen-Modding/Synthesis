using System.Reactive.Linq;
using Autofac;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Patchers.Cli;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli
{
    public class CliPatcherVm : PatcherVm, ICliInputSourceVm
    {
        public IPathToExecutableInputVm ExecutableInput { get; }
        public IShowHelpSetting ShowHelpSetting { get; }

        private readonly ObservableAsPropertyHelper<ConfigurationState> _State;
        public override ConfigurationState State => _State?.Value ?? ConfigurationState.Success;

        public CliPatcherVm(
            IPatcherIdProvider idProvider,
            IPatcherNameVm nameVm,
            IPathToExecutableInputVm pathToExecutableInputVm,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            IShowHelpSetting showHelpSetting,
            ILifetimeScope scope,
            CliPatcherSettings? settings = null)
            : base(scope, nameVm, selPatcher, confirmation, idProvider, settings)
        {
            ExecutableInput = pathToExecutableInputVm;
            ShowHelpSetting = showHelpSetting;

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

        public override PatcherSettings Save()
        {
            var ret = new CliPatcherSettings();
            CopyOverSave(ret);
            ret.PathToExecutable = ExecutableInput.Picker.TargetPath;
            return ret;
        }
    }
}
