using Noggog;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;

namespace Synthesis.Bethesda.GUI
{
    public abstract class PatcherInitVM : ViewModel
    {
        [Reactive]
        public string DisplayName { get; set; } = string.Empty;

        public ProfileVM Profile { get; }

        public abstract ErrorResponse CanCompleteConfiguration { get; }

        public abstract IAsyncEnumerable<PatcherVM> Construct();

        public PatcherInitVM(ProfileVM profile)
        {
            Profile = profile;
        }

        public virtual void Cancel()
        {
        }
    }
}
