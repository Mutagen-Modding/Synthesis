using System.Collections.Generic;
using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.GUI.Settings;

public interface IRetrieveSaveSettings
{
    void Retrieve(out SynthesisGuiSettings gui, out PipelineSettings pipe);
}

public class RetrieveSaveSettings : IRetrieveSaveSettings
{
    private readonly ISettingsSingleton _settingsSingleton;
    private readonly IEnumerable<IModifySavingSettings> _savers;
        
    public RetrieveSaveSettings(
        ISettingsSingleton settingsSingleton,
        IEnumerable<IModifySavingSettings> savers)
    {
        _settingsSingleton = settingsSingleton;
        _savers = savers;
    }
        
    public void Retrieve(out SynthesisGuiSettings gui, out PipelineSettings pipe)
    {
        gui = new();
        pipe = new();
        foreach (var saver in _savers)
        {
            saver.Save(gui, pipe);
        }
        gui.WorkingDirectory = _settingsSingleton.Gui.WorkingDirectory;
    }
}