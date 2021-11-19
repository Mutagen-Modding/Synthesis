using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Confirmations
{
    public class PatcherInitRenameActionVm : ViewModel, IConfirmationActionVm
    {
        public ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
        public ReactiveCommand<Unit, Unit>? DiscardActionCommand => null;
        public string Title => "Duplicate Patcher Name";
        public string Description => "Give the patcher a new unique name";
        
        [Reactive] public string Name { get; set; }

        public delegate PatcherInitRenameActionVm Factory(
            PatcherVm patcher,
            IReadOnlyCollection<string> existingNames);

        public PatcherInitRenameActionVm(
            IPatcherInitializationVm init,
            PatcherVm patcher,
            IReadOnlyCollection<string> existingNames)
        {
            Name = patcher.NameVm.Name;
            ConfirmActionCommand = ReactiveCommand.Create(
                () =>
                {
                    patcher.NameVm.Nickname = Name;
                    init.AddNewPatchers(patcher.AsEnumerable<PatcherVm>().ToList());
                },
                this.WhenAnyValue(x => x.Name).Select(n => !existingNames.Contains(n)));
        }
    }
}