using System.Collections.Generic;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Patchers.Cli;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization
{
    public class CliPatcherInitVm : PatcherInitVm, ICliInputSourceVm
    {
        public IPatcherFactory Factory { get; }
        public IPatcherNameVm NameVm { get; }
        public IPathToExecutableInputVm ExecutableInput { get; }

        public IShowHelpSetting ShowHelpSetting { get; }

        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public CliPatcherInitVm(
            IPatcherNameVm nameVm, 
            IPatcherInitializationVm init,
            IShowHelpSetting showHelpSetting,
            IPathToExecutableInputVm executableInputVm,
            IPatcherFactory factory)
            : base(init)
        {
            Factory = factory;
            NameVm = nameVm;
            ShowHelpSetting = showHelpSetting;
            ExecutableInput = executableInputVm;
            _CanCompleteConfiguration = executableInputVm.WhenAnyValue(x => x.Picker.ErrorState)
                .Cast<ErrorResponse, ErrorResponse>()
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);
        }

        public override async IAsyncEnumerable<PatcherVm> Construct()
        {
            yield return Factory.GetCliPatcher(new CliPatcherSettings()
            {
                PathToExecutable = ExecutableInput.Picker.TargetPath
            });
        }
    }
}
