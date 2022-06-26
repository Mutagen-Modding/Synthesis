using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Json;

public interface IGuiSettingsImporter
{
    ISynthesisGuiSettings Import(FilePath path);
}

public class GuiSettingsImporter : IGuiSettingsImporter
{
    private readonly IFileSystem _fileSystem;

    public GuiSettingsImporter(
        IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public ISynthesisGuiSettings Import(FilePath path)
    {
        return JsonConvert.DeserializeObject<SynthesisGuiSettings>(
            _fileSystem.File.ReadAllText(path),
            Synthesis.Bethesda.Execution.Constants.JsonSettings)!;
    }
}