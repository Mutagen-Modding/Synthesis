using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System.Collections.Generic;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI
{
    public class CliPatcherInitVM : PatcherInitVM
    {
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public CliPatcherVM Patcher { get; }

        public CliPatcherInitVM(
            PatcherInitializationVM init, 
            IScopeTracker<ProfileVM> profileProvider,
            IProfileDisplayControllerVm profileDisplay,
            IConfirmationPanelControllerVm confirmation,
            IShowHelpSetting showHelp)
            : base(init)
        {
            Patcher = new CliPatcherVM(
                profileProvider.Item,
                profileDisplay,
                confirmation,
                showHelp);
            _CanCompleteConfiguration = Patcher.WhenAnyValue(x => x.PathToExecutable.ErrorState)
                .Cast<ErrorResponse, ErrorResponse>()
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);
        }

        public override async IAsyncEnumerable<PatcherVM> Construct()
        {
            yield return Patcher;
        }
    }
}
