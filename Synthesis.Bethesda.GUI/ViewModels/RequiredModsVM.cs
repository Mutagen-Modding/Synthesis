using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class RequiredModsVM : ViewModel
    {
        [Reactive]
        public ModKey AddRequiredModKey { get; set; } = ModKey.Null;

        public ICommand AddRequiredModCommand { get; }

        public IObservableCollection<RequiredModVM> RequiredModsDisplay { get; }

        public IObservableCollection<DetectedModVM> DetectedMods { get; }

        public ICommand ClearSearchCommand { get; }

        [Reactive]
        public string DetectedModsSearch { get; set; } = string.Empty;

        public SourceCache<ModKey, ModKey> RequiredMods { get; } = new SourceCache<ModKey, ModKey>(x => x);

        public RequiredModsVM(IObservable<IChangeSet<LoadOrderEntryVM>> detectedLoadOrder)
        {
            RequiredModsDisplay = RequiredMods.Connect()
                .Sort(ModKey.Alphabetical, SortOptimisations.ComparesImmutableValuesOnly, resetThreshold: 0)
                .Transform(x => new RequiredModVM(x, this))
                .ToObservableCollection(this.CompositeDisposable);

            DetectedMods = detectedLoadOrder
                .Transform(x => x.Listing.ModKey)
                .AddKey(x => x)
                .Except(RequiredMods.Connect())
                .Filter(this.WhenAnyValue(x => x.DetectedModsSearch)
                    .Debounce(TimeSpan.FromMilliseconds(350), RxApp.MainThreadScheduler)
                    .Select(x => x.Trim())
                    .DistinctUntilChanged()
                    .Select(search =>
                    {
                        if (string.IsNullOrWhiteSpace(search))
                        {
                            return new Func<ModKey, bool>(_ => true);
                        }
                        return new Func<ModKey, bool>(
                            (p) =>
                            {
                                if (p.FileName.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
                                return false;
                            });
                    }))
                .Transform(x => new DetectedModVM(x, this))
                .ToObservableCollection(this.CompositeDisposable);

            AddRequiredModCommand = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.AddRequiredModKey),
                canExecute: x => !x.IsNull,
                execute: x =>
                {
                    RequiredMods.AddOrUpdate(x);
                    AddRequiredModKey = ModKey.Null;
                },
                disposable: this.CompositeDisposable);

            ClearSearchCommand = ReactiveCommand.Create(() => DetectedModsSearch = string.Empty);
        }
    }
}
