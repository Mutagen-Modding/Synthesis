using Mutagen.Bethesda;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace Synthesis.Bethesda.GUI
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
            Log.Logger.Information($"Watching {path}, current exist check: {exists}");
            _Exists = Observable.Defer(() => 
                Noggog.ObservableExt.WatchFile(path)
                    .Select(_ =>
                    {
                        var ret = File.Exists(path);
                        Log.Logger.Information($"{ret} file exists check for {path}");
                        return ret;
                    }))
                .ToGuiProperty(this, nameof(Exists), initialValue: exists);
        }
    }
}
