using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class CliPatcherInitVM : PatcherInitVM
    {
        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public CliPatcherVM Patcher { get; }

        public CliPatcherInitVM(PatcherInitializationVM init, ProfileVM profile)
            : base(init, profile)
        {
            Patcher = new CliPatcherVM(profile);
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
