using System.IO.Abstractions;
using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;

public class NotExistsError : INugetErrorSolution
{
    private readonly IFileSystem _fileSystem;
    public virtual string ErrorText => $"Config did not exist or was empty.";

    public static XDocument TypicalFile()
    {
        var elem = new XElement("configuration",
            new XElement("packageSources",
                new XElement("add",
                    new XAttribute("key", "nuget.org"),
                    new XAttribute("value", "https://api.nuget.org/v3/index.json"),
                    new XAttribute("protocolVersion", "3"))));
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            elem);
    }

    public NotExistsError(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public void RunFix(FilePath path)
    {
        using var stream = _fileSystem.FileStream.Create(path, FileMode.Create, FileAccess.Write);
        TypicalFile().Save(stream);
    }
}