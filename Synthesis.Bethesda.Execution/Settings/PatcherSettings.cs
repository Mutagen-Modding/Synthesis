using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public abstract class PatcherSettings
    {
        public bool On;
        public string Nickname = string.Empty;

        public abstract void Print(IRunReporter logger);
    }
}
