using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class CliPatcherSettings : PatcherSettings
    {
        public string PathToExecutable { get; set; } = string.Empty;
    }
}
