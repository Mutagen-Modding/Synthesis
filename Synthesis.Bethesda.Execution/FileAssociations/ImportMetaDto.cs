using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;

namespace Synthesis.Bethesda.Execution.FileAssociations;

public class ImportMetaDto
{
    private readonly IFileSystem _fileSystem;

    public ImportMetaDto(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public MetaFileDto Import(FilePath path)
    {
        return JsonConvert.DeserializeObject<MetaFileDto>(
            _fileSystem.File.ReadAllText(path), 
            Execution.Constants.JsonSettings)!;
    }
}