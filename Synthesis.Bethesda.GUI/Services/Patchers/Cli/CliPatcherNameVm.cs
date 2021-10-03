using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Cli
{
    public class CliPatcherNameVm : ViewModel, IPatcherNameVm
    {
        private readonly ICliNameConverter _cliNameConverter;
        
        private readonly ObservableAsPropertyHelper<string> _Name;
        public string Name => _Name.Value;

        public CliPatcherNameVm(
            ICliNameConverter cliNameConverter,
            IPathToExecutableInputVm pathToExecutableInputVm)
        {
            _cliNameConverter = cliNameConverter;
            _Name = pathToExecutableInputVm.WhenAnyValue(x => x.Picker.TargetPath)
                .Select(x => _cliNameConverter.Convert(x))
                .ToGuiProperty<string>(this, nameof(Name), string.Empty);
        }
    }
}