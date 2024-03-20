using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Builder;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.DotNet.ExecutablePath;

public interface IQueryExecutablePath
{
    Task<GetResponse<string>> Query(FilePath projectPath, CancellationToken cancel);
}

public class QueryExecutablePath : IQueryExecutablePath
{
    private readonly ILogger _Logger;
    public ISynthesisSubProcessRunner Runner { get; }
    public IRetrieveExecutablePath RetrievePath { get; }
    public IBuildStartInfoProvider StartInfoProvider { get; }

    public QueryExecutablePath(
        ILogger logger,
        ISynthesisSubProcessRunner processRunner,
        IRetrieveExecutablePath retrieveExecutablePath,
        IBuildStartInfoProvider buildStartInfoProvider)
    {
        _Logger = logger;
        Runner = processRunner;
        RetrievePath = retrieveExecutablePath;
        StartInfoProvider = buildStartInfoProvider;
    }
        
    public async Task<GetResponse<string>> Query(FilePath projectPath, CancellationToken cancel)
    {
        var result = await Runner.RunAndCapture(
            StartInfoProvider.Construct(projectPath),
            cancel: cancel).ConfigureAwait(false);
        if (result.Errors.Count > 0)
        {
            return GetResponse<string>.Fail($"{string.Join(Environment.NewLine, result.Errors)}");
        }
        if (!RetrievePath.TryGet(projectPath, result.Out, out var path))
        {
            _Logger.Warning("Could not locate target executable: {Lines}", string.Join(Environment.NewLine, result.Out));
            return GetResponse<string>.Fail("Could not locate target executable.");
        }
        return GetResponse<string>.Succeed(path);
    }
}