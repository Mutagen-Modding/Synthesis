using Serilog;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.DotNet;

public class PrintDotNetInfo
{
    private readonly ILogger _logger;
    private readonly IDotNetCommandPathProvider _dotNetCommandPathProvider;
    private readonly ISynthesisSubProcessRunner _processRunner;

    public PrintDotNetInfo(
        ILogger logger,
        IDotNetCommandPathProvider dotNetCommandPathProvider,
        ISynthesisSubProcessRunner processRunner)
    {
        _logger = logger;
        _dotNetCommandPathProvider = dotNetCommandPathProvider;
        _processRunner = processRunner;
    }

    public async Task Print(CancellationToken cancel)
    {
        await _processRunner.RunWithCallback(
            new System.Diagnostics.ProcessStartInfo(_dotNetCommandPathProvider.Path, "--info"),
            (s) => _logger.Information(s),
            cancel: cancel).ConfigureAwait(false);
    }
}