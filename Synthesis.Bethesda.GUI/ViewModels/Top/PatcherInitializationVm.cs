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
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public interface IPatcherInitializationVm
    {
        ICommand CompleteConfiguration { get; }
        ICommand CancelConfiguration { get; }
        PatcherInitVm? NewPatcher { get; set; }
        void AddNewPatchers(List<PatcherVm> patchersToAdd);
    }

    public class PatcherInitializationVm : ViewModel, IPatcherInitializationVm
    {
        private readonly ISelectedProfileControllerVm _SelectedProfile;
        
        public ICommand CompleteConfiguration { get; }
        
        public ICommand CancelConfiguration { get; }

        [Reactive]
        public PatcherInitVm? NewPatcher { get; set; }

        public PatcherInitializationVm(
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

        public void AddNewPatchers(List<PatcherVm> patchersToAdd)
        {
            NewPatcher = null;
            if (patchersToAdd.Count == 0) return;
            if (_SelectedProfile.SelectedProfile == null)
            {
                throw new ArgumentNullException("Selected profile unexpectedly null");
            }
            patchersToAdd.ForEach(p => p.IsOn = true);
            _SelectedProfile.SelectedProfile.Patchers.AddRange(patchersToAdd);
            _SelectedProfile.SelectedProfile.DisplayController.SelectedObject = patchersToAdd.First();
        }
    }
}