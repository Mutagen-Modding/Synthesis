using Noggog;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI
{
    public abstract class ASolutionInitializer : ViewModel
    {
        public delegate Task<IEnumerable<SolutionPatcherVm>> InitializerCall();
        public abstract IObservable<GetResponse<InitializerCall>> InitializationCall { get; }
    }
}
