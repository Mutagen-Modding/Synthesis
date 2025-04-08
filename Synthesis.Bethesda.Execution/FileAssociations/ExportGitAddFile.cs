using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.FileAssociations;

public class ExportGitAddFile
{
    private readonly IFileSystem _fileSystem;

    private const string Comment = @"/*
* This file is to be used with the Synthesis UI.
*
* To use this file, first install and download Synthesis:
* https://github.com/Mutagen-Modding/Synthesis/releases
*
* Then double clicking this file will add the desired patcher to the application.  
* This file alone does not do much and is not necessary.  It is just one way of many to add patchers to Synthesis.
* 
* More detailed instructions can be found here:
* https://github.com/Mutagen-Modding/Synthesis/wiki/Typical-Usage#using-synth-files
*
*/";

    public ExportGitAddFile(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public void ExportAsFile(
        FilePath path,
        string url, 
        string selectedSubPath,
        PatcherVersioningEnum versioning)
    {
        var dto = new MetaFileDto()
        {
            AddGitPatcher = new AddGitPatcherInstruction()
            {
                Url = url,
                SelectedProject = selectedSubPath,
                Versioning = versioning
            }
        };
        _fileSystem.File.WriteAllText(
            path, 
            Comment + Environment.NewLine + Environment.NewLine + 
            JsonConvert.SerializeObject(dto, Formatting.Indented, Execution.Constants.JsonSettings));
    }
}