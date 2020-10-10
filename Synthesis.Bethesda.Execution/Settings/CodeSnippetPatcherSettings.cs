using Noggog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class CodeSnippetPatcherSettings : PatcherSettings
    {
        public string Code = string.Empty;
        public string ID = string.Empty;

        public override void Print(ILogger logger)
        {
            logger.Write($"[Snippet] {Nickname.Decorate(x => $"{x} => ")}{ID}");
        }
    }
}
