using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;

namespace Synthesis.Bethesda.Execution.FileAssociations;

public class ExportGitAddFile
{
    private readonly IFileSystem _fileSystem;

    public ExportGitAddFile(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public void ExportAsFile(
        FilePath path,
        string url, 
        string selectedSubPath)
    {
        var dto = new MetaFileDto()
        {
            AddGitPatcher = new AddGitPatcherInstruction()
            {
                Url = url,
                SelectedProject = selectedSubPath
            }
        };
        _fileSystem.File.WriteAllText(
            path, 
            JsonConvert.SerializeObject(dto, Formatting.Indented, Execution.Constants.JsonSettings));
    }
}