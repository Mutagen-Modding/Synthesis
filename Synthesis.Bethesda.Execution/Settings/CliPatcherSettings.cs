using Noggog;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class CliPatcherSettings : PatcherSettings
    {
        public string PathToExecutable { get; set; } = string.Empty;

        public override void Print(IRunReporter logger)
        {
            logger.Write(default, $"[CLI] {Nickname.Decorate(x => $"{x} => ")}{PathToExecutable}");
        }
    }
}
