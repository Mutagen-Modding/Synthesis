using DynamicData;
using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

namespace Mutagen.Bethesda.Synthesis
{
    public abstract class PatcherVM : ViewModel
    {
        public MainVM MVM { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        [Reactive]
        public bool IsOn { get; set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public abstract string DisplayName { get; }

        [Reactive]
        public bool InInitialConfiguration { get; set; }

        protected virtual IObservable<ErrorResponse> CanCompleteConfiguration => Observable.Return(ErrorResponse.Success);

        public ICommand CompleteConfiguration { get; }
        public ICommand CancelConfiguration { get; }

        public abstract bool NeedsConfiguration { get; }

        public PatcherVM(MainVM mvm, PatcherSettings? settings)
        {
            MVM = mvm;
            _IsSelected = this.WhenAnyValue(x => x.MVM.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            CompleteConfiguration = ReactiveCommand.Create(
                () =>
                {
                    InInitialConfiguration = false;
                    mvm.Patchers.Add(this);
                },
                canExecute: Observable.CombineLatest(
                    this.WhenAnyValue(x => x.InInitialConfiguration),
                    CanCompleteConfiguration.Select(e => e.Succeeded),
                    (inConfig, success) => inConfig && success));

            CancelConfiguration = ReactiveCommand.Create(
                () =>
                {
                    // Just forget about us and let us GC
                    mvm.SelectedPatcher = null;
                },
                canExecute: this.WhenAnyValue(x => x.InInitialConfiguration));

            // Set to settings
            IsOn = settings?.On ?? false;
        }

        public abstract PatcherSettings Save();

        protected void CopyOverSave(PatcherSettings settings)
        {
            settings.On = IsOn;
        }
    }
}
