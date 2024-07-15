using System.IO.Abstractions;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject;

public interface IProcessProjUsings
{
    void Process(string projPath);
}

public class ProcessProjUsings : IProcessProjUsings
{
    private readonly IFileSystem _fs;

    public ProcessProjUsings(IFileSystem fs)
    {
        _fs = fs;
    }
        
    public void Process(string projPath)
    {
        foreach (var cs in _fs.Directory.EnumerateFiles(Path.GetDirectoryName(projPath)!, "*.cs", SearchOption.AllDirectories))
        {
            var lines = _fs.File.ReadAllLines(cs);
            if (lines.Any(l =>
                {
                    if (l.StartsWith("using Mutagen.Bethesda", StringComparison.Ordinal)) return true;
                    if (l.StartsWith("namespace Mutagen.Bethesda", StringComparison.Ordinal)) return true;
                    if (l.Contains("FormLink", StringComparison.Ordinal)) return true;
                    if (l.Contains("ModKey", StringComparison.Ordinal)) return true;
                    return false;
                }))
            {
                _fs.File.WriteAllLines(
                    cs,
                    "using Mutagen.Bethesda.Plugins.Records;".AsEnumerable()
                        .And("using Mutagen.Bethesda.Plugins;")
                        .And("using Mutagen.Bethesda.Plugins.Order;")
                        .And("using Mutagen.Bethesda.Plugins.Aspects;")
                        .And("using Mutagen.Bethesda.Plugins.Cache;")
                        .And("using Mutagen.Bethesda.Plugins.Exceptions;")
                        .And("using Mutagen.Bethesda.Plugins.Binary;")
                        .And("using Mutagen.Bethesda.Archives;")
                        .And("using Mutagen.Bethesda.Strings;")
                        .And(lines.Where(x => x != "using Mutagen.Bethesda.Bsa;")));
            }
        }
    }
}