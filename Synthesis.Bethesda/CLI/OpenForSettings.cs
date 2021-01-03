using CommandLine;
using Mutagen.Bethesda;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda
{
    [Verb("open-for-settings", HelpText = "Informs the patcher to open in settings mode, if it supports it.")]
    public class OpenForSettings
    {
        public override string ToString()
        {
            return $"{nameof(OpenForSettings)}";
        }
    }
}
