using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Git;

public interface IBuildMetaFileReader
{
    GitCompilationMeta? Read(FilePath path);
}

public class BuildMetaFileReader : IBuildMetaFileReader
{
    private readonly IFileSystem _fs;

    public BuildMetaFileReader(IFileSystem fs)
    {
        _fs = fs;
    }
        
    public GitCompilationMeta? Read(FilePath path)
    {
        if (!_fs.File.Exists(path)) return null;
        return JsonConvert.DeserializeObject<GitCompilationMeta>(_fs.File.ReadAllText(path), Constants.JsonSettings)!;
    }
}