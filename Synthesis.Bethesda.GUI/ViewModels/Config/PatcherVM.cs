using DynamicData;
using Synthesis.Bethesda.Execution.Settings;
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

namespace Synthesis.Bethesda.GUI
{
    public abstract class PatcherVM : ViewModel
    {
        public ConfigurationVM Config { get; }

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

        public PatcherVM(ConfigurationVM parent, PatcherSettings? settings)
        {
            Config = parent;
            _IsSelected = this.WhenAnyValue(x => x.Config.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            CompleteConfiguration = ReactiveCommand.Create(
                () =>
                {
                    InInitialConfiguration = false;
                    Config.Patchers.Add(this);
                },
                canExecute: Observable.CombineLatest(
                    this.WhenAnyValue(x => x.InInitialConfiguration),
                    CanCompleteConfiguration.Select(e => e.Succeeded),
                    (inConfig, success) => inConfig && success));

            CancelConfiguration = ReactiveCommand.Create(
                () =>
                {
                    // Just forget about us and let us GC
                    Config.SelectedPatcher = null;
                },
                canExecute: this.WhenAnyValue(x => x.InInitialConfiguration));

            // Set to settings
            IsOn = settings?.On ?? false;
            Nickname = settings?.Nickname ?? string.Empty;
        }

        public abstract PatcherSettings Save();

        protected void CopyOverSave(PatcherSettings settings)
        {
            settings.On = IsOn;
            settings.Nickname = Nickname;
        }
    }
}
