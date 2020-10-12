using Noggog;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class CodeSnippetPatcherSettings : PatcherSettings
    {
        public string Code = string.Empty;
        public string ID = string.Empty;

        public override void Print(IRunReporter logger)
        {
            logger.Write(default, $"[Snippet] {Nickname.Decorate(x => $"{x} => ")}{ID}");
        }
    }
}
