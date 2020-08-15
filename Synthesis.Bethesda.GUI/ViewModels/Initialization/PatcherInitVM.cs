using Noggog;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public abstract class PatcherInitVM : ViewModel
    {
        public abstract ErrorResponse CanCompleteConfiguration { get; }
        public abstract PatcherVM Patcher { get; }
        public virtual async Task ExecuteChanges()
        {
        }
    }
}
