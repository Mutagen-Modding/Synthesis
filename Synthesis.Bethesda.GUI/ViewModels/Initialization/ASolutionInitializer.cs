using Noggog;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StructureMap;

namespace Synthesis.Bethesda.GUI
{
    public abstract class ASolutionInitializer : ViewModel
    {
        public delegate Task<IEnumerable<SolutionPatcherVM>> InitializerCall(IContainer container);
        public abstract IObservable<GetResponse<InitializerCall>> InitializationCall { get; }
    }
}
