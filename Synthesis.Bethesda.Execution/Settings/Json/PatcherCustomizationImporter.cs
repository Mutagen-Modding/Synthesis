using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.DTO;

namespace Synthesis.Bethesda.Execution.Settings.Json;

public interface IPatcherCustomizationImporter
{
    PatcherCustomization Import(FilePath path);
}

public class PatcherCustomizationImporter : IPatcherCustomizationImporter
{
    private readonly IFileSystem _fileSystem;

    public PatcherCustomizationImporter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public PatcherCustomization Import(FilePath path)
    {
        return JsonConvert.DeserializeObject<PatcherCustomization>(
            _fileSystem.File.ReadAllText(path),
            Execution.Constants.JsonSettings)!;
    }
}