using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    [ExcludeFromCodeCoverage]
    public abstract class PatcherSettings
    {
        public bool On;
        public string Nickname = string.Empty;

        public abstract void Print(IRunReporter logger);
    }
}
