using Noggog;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.Execution.Settings
{
    public class SolutionPatcherSettings : PatcherSettings
    {
        public string SolutionPath = string.Empty;
        public string ProjectSubpath = string.Empty;

        public override void Print(IRunReporter logger)
        {
            logger.Write(null, default, $"[Solution] {Nickname.Decorate(x => $"{x} => ")}{SolutionPath} => {ProjectSubpath}");
        }
    }
}
