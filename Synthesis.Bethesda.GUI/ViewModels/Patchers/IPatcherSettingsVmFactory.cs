using System;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers
{
    public interface IPatcherSettingsVmFactory
    {
        PatcherSettingsVM Create(
            PatcherVM parent,
            ILogger logger, 
            bool needBuild,
            IObservable<(GetResponse<FilePath> ProjPath, string? SynthVersion)> source);
    }
}