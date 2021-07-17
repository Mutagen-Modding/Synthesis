using System.Collections.Generic;
using Noggog;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization
{
    public abstract class PatcherInitVm : ViewModel
    {
        [Reactive]
        public string DisplayName { get; set; } = string.Empty;
        
        public IPatcherInitializationVm Init { get; }

        public abstract ErrorResponse CanCompleteConfiguration { get; }

        public abstract IAsyncEnumerable<PatcherVm> Construct();

        public PatcherInitVm(IPatcherInitializationVm init)
        {
            Init = init;
        }

        public virtual void Cancel()
        {
        }
    }
}
