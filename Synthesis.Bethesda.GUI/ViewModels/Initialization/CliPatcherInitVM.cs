using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class CliPatcherInitVM : PatcherInitVM
    {
        private readonly CliPatcherVM _patcher;
        public override PatcherVM Patcher => _patcher;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        public CliPatcherInitVM(CliPatcherVM patcher)
        {
            _patcher = patcher;

            _CanCompleteConfiguration = _patcher.WhenAnyValue(x => x.PathToExecutable.ErrorState)
                .Cast<ErrorResponse, ErrorResponse>()
                .ToGuiProperty(this, nameof(CanCompleteConfiguration), ErrorResponse.Success);
        }
    }
}
