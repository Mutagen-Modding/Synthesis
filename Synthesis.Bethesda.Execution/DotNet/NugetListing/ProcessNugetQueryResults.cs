using System.Collections.Generic;
using Synthesis.Bethesda.Execution.DotNet.Dto;

namespace Synthesis.Bethesda.Execution.DotNet.NugetListing
{
    public interface IProcessNugetQueryResults
    {
        IEnumerable<NugetListingQuery> Process(IReadOnlyList<string> output);
    }

    public class ProcessNugetQueryResults : IProcessNugetQueryResults
    {
        public const string Prefix = "   > ";
        public INugetListingParser LineParser { get; }
        
        public ProcessNugetQueryResults(
            INugetListingParser lineParser)
        {
            LineParser = lineParser;
        }
        
        public IEnumerable<NugetListingQuery> Process(IReadOnlyList<string> output)
        {
            var lines = new List<string>();
            foreach (var s in output)
            {
                if (!s.StartsWith(Prefix)) continue;
                lines.Add(s);
            }

            var ret = new List<NugetListingQuery>();
            foreach (var line in lines)
            {
                if (!LineParser.TryParse(
                    line, 
                    out var package,
                    out var requested, 
                    out var resolved, 
                    out var latest))
                {
                    continue;
                }
                ret.Add(new(package, requested, resolved, latest));
            }
            return ret;
        }
    }
}