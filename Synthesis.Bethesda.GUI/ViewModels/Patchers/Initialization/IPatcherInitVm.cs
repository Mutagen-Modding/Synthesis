using System.Collections.Generic;
using System.Windows.Input;
using Noggog;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization
{
    public interface IPatcherInitVm : IDisposableDropoff
    {
        ICommand CompleteConfiguration { get; }
        ICommand CancelConfiguration { get; }
        ErrorResponse CanCompleteConfiguration { get; }
        IAsyncEnumerable<PatcherInputVm> Construct();
        void Cancel();
    }
}