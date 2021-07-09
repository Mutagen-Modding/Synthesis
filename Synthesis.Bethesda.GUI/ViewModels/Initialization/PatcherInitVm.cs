using Noggog;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;

namespace Synthesis.Bethesda.GUI
{
    public abstract class PatcherInitVm : ViewModel
    {
        [Reactive]
        public string DisplayName { get; set; } = string.Empty;
        
        public PatcherInitializationVm Init { get; }

        public abstract ErrorResponse CanCompleteConfiguration { get; }

        public abstract IAsyncEnumerable<PatcherVm> Construct();

        public PatcherInitVm(PatcherInitializationVm init)
        {
            Init = init;
        }

        public virtual void Cancel()
        {
        }
    }
}
