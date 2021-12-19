using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Cli
{
    public class CliPatcherNameVm : ViewModel, IPatcherNameVm
    {
        private readonly ICliNameConverter _cliNameConverter;
        
        private readonly ObservableAsPropertyHelper<string> _Name;
        public string Name => _Name.Value;

        [Reactive] public string Nickname { get; set; } = string.Empty;

        public CliPatcherNameVm(
            ICliNameConverter cliNameConverter,
            IPathToExecutableInputVm pathToExecutableInputVm)
        {
            _cliNameConverter = cliNameConverter;
            _Name = pathToExecutableInputVm.WhenAnyValue(x => x.Picker.TargetPath)
                .Select(x => _cliNameConverter.Convert(x))
                .CombineLatest(
                    this.WhenAnyValue(x => x.Nickname),
                    (auto, nickname) => nickname.IsNullOrWhitespace() ? auto : nickname)
                .ToGuiProperty<string>(this, nameof(Name), string.Empty, deferSubscription: true);
        }
    }
}