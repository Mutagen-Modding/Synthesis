using System;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget
{
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
            
            var doc = XElement.Load(_fileSystem.FileStream.Create(path, FileMode.Open, FileAccess.Read));
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
                    .NotNull()
                    .Any(attr => attr.Value.Equals("https://api.nuget.org/v3/index.json")))
                {
                    return;
                }

                sources.Add(addElem);
            }
                        
            doc.Save(_fileSystem.FileStream.Create(path, FileMode.Create, FileAccess.Write));
        }
    }
}