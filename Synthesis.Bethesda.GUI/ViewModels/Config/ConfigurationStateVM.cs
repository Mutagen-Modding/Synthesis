using Noggog;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationStateVM : ViewModel
    {
        public static readonly ConfigurationStateVM Success = new ConfigurationStateVM();

        public bool IsHaltingError { get; set; }
        public ErrorResponse RunnableState { get; set; } = ErrorResponse.Success;
    }
}
