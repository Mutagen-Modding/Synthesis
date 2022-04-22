using System.IO.Abstractions;
using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;

public interface IAnalyzeNugetConfig
{
    INugetErrorSolution? Analyze(FilePath path);
}

public class AnalyzeNugetConfig : IAnalyzeNugetConfig
{
    private readonly IFileSystem _fileSystem;

    public AnalyzeNugetConfig(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public INugetErrorSolution? Analyze(FilePath path)
    {
        if (!_fileSystem.File.Exists(path))
        {
            return new NotExistsError(_fileSystem);
        }

        if (_fileSystem.File.ReadAllLines(path).All(x => x.IsNullOrWhitespace()))
        {
            return new NotExistsError(_fileSystem);
        }

        using var stream = _fileSystem.FileStream.Create(path, FileMode.Open, FileAccess.Read);
        XDocument doc;
        try
        {
            doc = XDocument.Load(stream);
        }
        catch (Exception e)
        {
            return new CorruptError(_fileSystem, e);
        }
                    
        var config = doc.Element("configuration");
        if (config == null)
        {
            return new NotExistsError(_fileSystem);
        }

        var sources = config.Element("packageSources");
        if (sources != null 
            && sources.Elements("add")
                .Select(x => x.Attribute("value"))
                .NotNull()
                .Any(attr => attr.Value.Equals("https://api.nuget.org/v3/index.json")))
        {
            return default;
        }

        return new MissingNugetOrgError(_fileSystem);
    }
}