using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Noggog;

namespace Synthesis.Bethesda.Execution.EnvironmentErrors.Nuget
{
    public static class NugetErrors
    {
        public static INugetErrorSolution? AnalyzeNugetConfig(FilePath path)
        {
            if (!path.Exists)
            {
                return NotExistsError.Instance;
            }

            if (File.ReadAllLines(path).All(x => x.IsNullOrWhitespace()))
            {
                return NotExistsError.Instance;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Load(path);
            }
            catch (Exception e)
            {
                return new CorruptError(e);
            }
                    
            var config = doc.Element("configuration");
            if (config == null)
            {
                return NotExistsError.Instance;
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

            return MissingNugetOrgError.Instance;
        }
    }
}