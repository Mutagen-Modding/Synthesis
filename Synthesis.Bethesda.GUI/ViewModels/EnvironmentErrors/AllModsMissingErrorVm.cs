using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using Mutagen.Bethesda.Plugins.Implicit.DI;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;
using Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

namespace Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors
{
    public class AllModsMissingErrorVm : ViewModel, IEnvironmentErrorVm
    {
        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<string?> _ErrorString;
        public string? ErrorString => _ErrorString.Value;

        private readonly ObservableAsPropertyHelper<string> _DataFolderPath;
        public string DataFolderPath => _DataFolderPath.Value;

        public ICommand GoToProfileSettingsCommand { get; }
        
        public AllModsMissingErrorVm(
            IProfileDataFolderVm dataFolderVm,
            OpenProfileSettings openProfileSettings,
            IImplicitListingModKeyProvider implicitListingsProvider,
            IProfileLoadOrder profileLoadOrder)
        {
            var nonImplicit = profileLoadOrder.LoadOrder.Connect()
                .Filter(x => !implicitListingsProvider.Listings.Contains(x.ModKey))
                .ObserveOn(RxApp.MainThreadScheduler)
                .AsObservableList();
            
            _InError = nonImplicit.CountChanged
                .Select(x => x > 0)
                .CombineLatest(
                    nonImplicit.Connect()
                        .FilterOnObservable(i => i.WhenAnyValue(x => x.Exists))
                        .QueryWhenChanged(q => q.Count > 0),
                    (hasAny, anyExist) => hasAny && !anyExist)
                .ToGuiProperty(this, nameof(InError), deferSubscription: true);

            _ErrorString = nonImplicit.CountChanged
                .Select(count =>
                {
                    return $"Load order listed {count} mods, but none were found in the game's data folder";
                })
                .ToGuiProperty(this, nameof(ErrorString), default(string?), deferSubscription: true);

            _DataFolderPath = dataFolderVm.WhenAnyValue(x => x.Path)
                .Select(x => x.Path)
                .ToGuiProperty(this, nameof(DataFolderPath), string.Empty, deferSubscription: true);

            GoToProfileSettingsCommand = openProfileSettings.OpenCommand;
        }
    }
}
