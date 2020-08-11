using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class OverallErrorVM : ViewModel
    {
        public Exception Exception { get; }

        public OverallErrorVM(Exception ex)
        {
            Exception = ex;
        }
    }
}
