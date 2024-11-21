﻿using System.IO.Abstractions;
using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget;

public class MissingNugetOrgError : INugetErrorSolution
{
    private readonly IFileSystem _fileSystem;
    public string ErrorText => "Config did not list nuget.org as a source";

    public MissingNugetOrgError(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
        
    public void RunFix(FilePath path)
    {
        var addElem = new XElement("add",
            new XAttribute("key", "nuget.org"),
            new XAttribute("value", "https://api.nuget.org/v3/index.json"),
            new XAttribute("protocolVersion", "3"));

        XElement doc;

        using (var stream = _fileSystem.FileStream.New(path, FileMode.Open, FileAccess.Read))
        {
            doc = XElement.Load(stream);
        }
        var sources = doc.Element("packageSources");
        if (sources == null)
        {
            doc.Add(
                new XElement("packageSources",
                    addElem));
        }
        else
        {

            if (sources.Elements("add")
                .Select(x => x.Attribute("value"))
                .WhereNotNull()
                .Any(attr => attr.Value.Equals("https://api.nuget.org/v3/index.json")))
            {
                return;
            }

            sources.Add(addElem);
        }

        using (var stream = _fileSystem.FileStream.New(path, FileMode.Create, FileAccess.Write))
        {
            doc.Save(stream);
        }
    }
}