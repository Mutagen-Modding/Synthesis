using Noggog;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;

namespace Synthesis.Bethesda.GUI
{
    public abstract class PatcherInitVM : ViewModel
    {
        [Reactive]
        public string DisplayName { get; set; } = string.Empty;
        
        public PatcherInitializationVM Init { get; }

        public abstract ErrorResponse CanCompleteConfiguration { get; }

        public abstract IAsyncEnumerable<PatcherVM> Construct();

        public PatcherInitVM(PatcherInitializationVM init)
        {
            Init = init;
        }

        public virtual void Cancel()
        {
        }
    }
}
