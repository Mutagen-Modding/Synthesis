using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public interface INugetVersioningVM
    {
        NugetVersioningEnum MutagenVersioning { get; set; }
        string ManualMutagenVersion { get; set; }
        NugetVersioningEnum SynthesisVersioning { get; set; }
        string ManualSynthesisVersion { get; set; }
        (string? MatchVersion, string? SelectedVersion) MutagenVersionDiff { get; }
        (string? MatchVersion, string? SelectedVersion) SynthesisVersionDiff { get; }
    }
}
