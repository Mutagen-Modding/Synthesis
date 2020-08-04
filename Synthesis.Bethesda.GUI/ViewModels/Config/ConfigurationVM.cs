using DynamicData;
using DynamicData.Binding;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationVM : ViewModel
    {
        public MainVM MainVM { get; }

        public SourceCache<ProfileVM, string> Profiles { get; } = new SourceCache<ProfileVM, string>(p => p.ID);

        public IObservableCollection<ProfileVM> ProfilesDisplay { get; }
        public IObservableCollection<PatcherVM> PatchersDisplay { get; }

        public ICommand CompleteConfiguration { get; }
        public ICommand CancelConfiguration { get; }

        [Reactive]
        public ProfileVM? SelectedProfile { get; set; }

        [Reactive]
        public PatcherVM? SelectedPatcher { get; set; }

        [Reactive]
        public PatcherVM? NewPatcher { get; set; }

        private readonly ObservableAsPropertyHelper<PatcherVM?> _DisplayedPatcher;
        public PatcherVM? DisplayedPatcher => _DisplayedPatcher.Value;

        public ConfigurationVM(MainVM mvm)
        {
            MainVM = mvm;
            ProfilesDisplay = Profiles.Connect().ToObservableCollection(this);
            PatchersDisplay = this.WhenAnyValue(x => x.SelectedProfile)
                .Select(p => p?.Patchers.Connect() ?? Observable.Empty<IChangeSet<PatcherVM>>())
                .Switch()
                .ToObservableCollection(this);

            CompleteConfiguration = ReactiveCommand.Create(
                () =>
                {
                    var patcher = this.NewPatcher;
                    if (patcher == null) return;
                    SelectedProfile?.Patchers.Add(patcher);
                    NewPatcher = null;
                    SelectedPatcher = patcher;
                    patcher.IsOn = true;
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
                    // Just forget about patcher and let it GC
                    NewPatcher = null;
                });

            _DisplayedPatcher = this.WhenAnyValue(
                    x => x.SelectedPatcher,
                    x => x.NewPatcher,
                    (selected, newConfig) => newConfig ?? selected)
                .ToGuiProperty(this, nameof(DisplayedPatcher));
        }

        public void Load(SynthesisSettings settings)
        {
            Profiles.Clear();
            Profiles.AddOrUpdate(settings.Profiles.Select(p =>
            {
                return new ProfileVM(this, p);
            }));
            if (Profiles.TryGetValue(settings.SelectedProfile, out var profile))
            {
                SelectedProfile = profile;
            }
        }

        public SynthesisSettings Save()
        {
            return new SynthesisSettings()
            {
                Profiles = Profiles.Items.Select(p => p.Save()).ToList(),
                SelectedProfile = SelectedProfile?.ID ?? string.Empty
            };
        }
    }
}
