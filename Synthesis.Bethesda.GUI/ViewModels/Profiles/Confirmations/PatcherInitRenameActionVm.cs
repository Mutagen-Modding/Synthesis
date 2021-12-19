using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Confirmations
{
    public class PatcherInitRenameActionVm : ViewModel, IConfirmationActionVm
    {
        public ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
        public ReactiveCommand<Unit, Unit>? DiscardActionCommand => null;
        public string Title => "Duplicate Patcher Name";
        public string Description => "Give the patcher a new unique name";
        
        [Reactive] public string Name { get; set; }

        public PatcherInitRenameActionVm(
            string currentName,
            IReadOnlyCollection<string> existingNames)
        {
            Name = currentName;
            ConfirmActionCommand = ReactiveCommand.Create(
                () => { },
                this.WhenAnyValue(x => x.Name).Select(n => !existingNames.Contains(n)));
        }
    }
}