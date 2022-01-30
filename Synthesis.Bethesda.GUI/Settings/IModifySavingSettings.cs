using Synthesis.Bethesda.Execution.Settings.V2;

namespace Synthesis.Bethesda.GUI.Settings;

public interface IModifySavingSettings
{
    void Save(SynthesisGuiSettings gui, PipelineSettings pipe);
}