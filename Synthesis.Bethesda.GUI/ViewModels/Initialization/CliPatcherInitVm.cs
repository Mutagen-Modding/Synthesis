using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System.Collections.Generic;
using Synthesis.Bethesda.GUI.ViewModels;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;

namespace Synthesis.Bethesda.GUI
{
    public class CliPatcherInitVm : PatcherInitVm
    {
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public CliPatcherVm Patcher { get; }

        public CliPatcherInitVm(
            IPatcherInitializationVm init, 
            CliPatcherVm patcher)
            : base(init)
        {
            Patcher = patcher;
            _CanCompleteConfiguration = Patcher.WhenAnyValue(x => x.PathToExecutable.ErrorState)
                .Cast<ErrorResponse, ErrorResponse>()
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);
        }

        public override async IAsyncEnumerable<PatcherVm> Construct()
        {
            yield return Patcher;
        }
    }
}
