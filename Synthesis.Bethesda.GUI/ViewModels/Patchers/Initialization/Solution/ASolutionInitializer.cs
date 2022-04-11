using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Noggog;
using Noggog.WPF;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution;

public abstract class ASolutionInitializer : ViewModel
{
    public delegate Task<IEnumerable<SolutionPatcherVm>> InitializerCall();
    public abstract IObservable<GetResponse<InitializerCall>> InitializationCall { get; }
}