using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Synthesis.Bethesda.Execution.DotNet.NugetListing;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface IQueryLibraryVersions
{
    Task<NugetVersionPair> Query(
        FilePath projectPath,
        bool current,
        bool includePrerelease,
        CancellationToken cancel);
}

public class QueryLibraryVersions : IQueryLibraryVersions
{
    private readonly IQueryNugetListing _queryNuget;

    public QueryLibraryVersions(IQueryNugetListing queryNuget)
    {
        _queryNuget = queryNuget;
    }
        
    public async Task<NugetVersionPair> Query(
        FilePath projectPath, bool current, bool includePrerelease, CancellationToken cancel)
    {
        string? mutagenVersion = null, synthesisVersion = null;
        var queries = await _queryNuget.Query(projectPath, outdated: !current, includePrerelease: includePrerelease, cancel: cancel).ConfigureAwait(false);
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