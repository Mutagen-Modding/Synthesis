using Mutagen.Bethesda.Synthesis.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public abstract class PatcherVM : ViewModel
    {
        public MainVM MVM { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        [Reactive]
        public bool IsOn { get; set; }

        public PatcherVM(MainVM mvm)
        {
            MVM = mvm;
            _IsSelected = this.WhenAnyValue(x => x.MVM.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));
        }

        public abstract PatcherSettings Save();
    }
}
