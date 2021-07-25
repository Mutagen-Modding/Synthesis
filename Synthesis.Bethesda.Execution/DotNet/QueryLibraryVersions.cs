using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Synthesis.Bethesda.Execution.DotNet.NugetListing;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public record LibraryVersions(string? MutagenVersion, string? SynthesisVersion);
    
    public interface IQueryLibraryVersions
    {
        Task<LibraryVersions> Query(FilePath projectPath, bool current, bool includePrerelease, CancellationToken cancel);
    }

    public class QueryLibraryVersions : IQueryLibraryVersions
    {
        private readonly IQueryNugetListing _QueryNuget;

        public QueryLibraryVersions(IQueryNugetListing queryNuget)
        {
            _QueryNuget = queryNuget;
        }
        
        public async Task<LibraryVersions> Query(
            FilePath projectPath, bool current, bool includePrerelease, CancellationToken cancel)
        {
            string? mutagenVersion = null, synthesisVersion = null;
            var queries = await _QueryNuget.Query(projectPath, outdated: !current, includePrerelease: includePrerelease, cancel: cancel);
            foreach (var item in queries)
            {
                if (item.Package.StartsWith("Mutagen.Bethesda")
                    && !item.Package.EndsWith("Synthesis"))
                {
                    mutagenVersion = current ? item.Resolved : item.Latest;
                }
                if (item.Package.Equals("Mutagen.Bethesda.Synthesis"))
                {
                    synthesisVersion = current ? item.Resolved : item.Latest;
                }
            }
            return new(mutagenVersion, synthesisVersion);
        }
    }
}