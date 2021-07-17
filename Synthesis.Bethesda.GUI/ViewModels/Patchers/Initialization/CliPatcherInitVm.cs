using System.Collections.Generic;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization
{
    public class CliPatcherInitVm : PatcherInitVm
    {
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public CliPatcherVm Patcher { get; }

        public CliPatcherInitVm(
            IPatcherInitializationVm init, 
            IPatcherFactory factory)
            : base(init)
        {
            Patcher = factory.GetCliPatcher();
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
