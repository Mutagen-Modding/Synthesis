using System.Diagnostics;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.DotNet;

public interface IQueryInstalledSdkListings
{
    Task<IReadOnlyList<string>> Query(CancellationToken cancel);
}

public class QueryInstalledSdkListings : IQueryInstalledSdkListings
{
    private readonly IDotNetCommandPathProvider _dotNetCommandPathProvider;
    private readonly ISynthesisSubProcessRunner _processRunner;

    public QueryInstalledSdkListings(
        IDotNetCommandPathProvider dotNetCommandPathProvider,
        ISynthesisSubProcessRunner processRunner)
    {
        _dotNetCommandPathProvider = dotNetCommandPathProvider;
        _processRunner = processRunner;
    }

    public async Task<IReadOnlyList<string>> Query(CancellationToken cancel)
    {
        var result = await _processRunner.RunAndCapture(
            new ProcessStartInfo(_dotNetCommandPathProvider.Path, "--list-sdks"),
            cancel: cancel).ConfigureAwait(false);
        return result.Out
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }
}
