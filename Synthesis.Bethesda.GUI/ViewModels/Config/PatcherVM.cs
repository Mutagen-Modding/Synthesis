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
        public ProfileVM Profile { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        [Reactive]
        public bool IsOn { get; set; } = true;

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public abstract string DisplayName { get; }

        private readonly ObservableAsPropertyHelper<bool> _InInitialConfiguration;
        public bool InInitialConfiguration => _InInitialConfiguration.Value;

        public abstract ErrorResponse CanCompleteConfiguration { get; }

        public abstract bool NeedsConfiguration { get; }

        public PatcherVM(ProfileVM parent, PatcherSettings? settings)
        {
            Profile = parent;
            _IsSelected = this.WhenAnyValue(x => x.Profile.Config.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            _InInitialConfiguration = this.WhenAnyValue(x => x.Profile.Config.NewPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(InInitialConfiguration));

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
