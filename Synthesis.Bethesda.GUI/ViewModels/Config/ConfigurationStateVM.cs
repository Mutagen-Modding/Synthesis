using Noggog;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationStateVM : ViewModel
    {
        public bool IsHaltingError { get; set; }
        public IErrorResponse RunnableState { get; set; } = ErrorResponse.Success;
    }
}
