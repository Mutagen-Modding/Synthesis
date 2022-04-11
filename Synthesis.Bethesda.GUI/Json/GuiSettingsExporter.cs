using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Json;

public interface IGuiSettingsExporter
{
    void Write(FilePath path, ISynthesisGuiSettings gui);
}

public class GuiSettingsExporter : IGuiSettingsExporter
{
    private readonly IFileSystem _fileSystem;

    public GuiSettingsExporter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public void Write(FilePath path, ISynthesisGuiSettings gui)
    {
        _fileSystem.File.WriteAllText(path,
            JsonConvert.SerializeObject(gui, Formatting.Indented, Execution.Constants.JsonSettings));
    }
}