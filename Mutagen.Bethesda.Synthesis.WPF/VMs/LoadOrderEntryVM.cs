using Mutagen.Bethesda.Plugins.Order;
using Noggog.WPF;
using ReactiveUI;
using System.IO;
using System.Reactive.Linq;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class LoadOrderEntryVM : ViewModel
    {
        public LoadOrderListing Listing {get;}

        private readonly ObservableAsPropertyHelper<bool> _Exists;
        public bool Exists => _Exists.Value;

        public LoadOrderEntryVM(LoadOrderListing listing, string dataFolder)
        {
            Listing = listing;
            var path = Path.Combine(dataFolder, listing.ModKey.FileName);
            var exists = File.Exists(path);
            _Exists = Observable.Defer(() => 
                Noggog.ObservableExt.WatchFile(path)
                    .Select(_ =>
                    {
                        var ret = File.Exists(path);
                        return ret;
                    }))
                .ToGuiProperty(this, nameof(Exists), initialValue: exists);
        }
    }
}
