using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI
{
    public class PatcherInitializationVM : ViewModel
    {
        private readonly ISelectedProfileControllerVm _SelectedProfile;
        public ICommand CompleteConfiguration { get; }
        
        public ICommand CancelConfiguration { get; }

        [Reactive]
        public PatcherInitVM? NewPatcher { get; set; }

        public PatcherInitializationVM(
            ISelectedProfileControllerVm selectedProfile)
        {
            _SelectedProfile = selectedProfile;
            CompleteConfiguration = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var initializer = this.NewPatcher;
                    if (initializer == null) return;
                    AddNewPatchers(await initializer.Construct().ToListAsync());
                },
                canExecute: this.WhenAnyValue(x => x.NewPatcher)
                    .Select(patcher =>
                    {
                        if (patcher == null) return Observable.Return(false);
                        return patcher.WhenAnyValue(x => x.CanCompleteConfiguration)
                            .Select(e => e.Succeeded);
                    })
                    .Switch());

            CancelConfiguration = ReactiveCommand.Create(
                () =>
                {
                    NewPatcher?.Cancel();
                    NewPatcher = null;
                });

            // Dispose any old patcher initializations
            this.WhenAnyValue(x => x.NewPatcher)
                .DisposePrevious()
                .Subscribe()
                .DisposeWith(this);
        }

        public void AddNewPatchers(List<PatcherVM> patchersToAdd)
        {
            NewPatcher = null;
            if (patchersToAdd.Count == 0) return;
            if (_SelectedProfile.SelectedProfile == null)
            {
                throw new ArgumentNullException("Selected profile unexpectedly null");
            }
            patchersToAdd.ForEach(p => p.IsOn = true);
            _SelectedProfile.SelectedProfile.Patchers.AddRange(patchersToAdd);
            _SelectedProfile.SelectedProfile.Container.GetInstance<IProfileDisplayControllerVm>()
                .SelectedObject = patchersToAdd.First();
        }
    }
}