using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Noggog.Utility;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.GUI.Services.Startup;

public class ShutDownBuildServer
{
    private readonly ILogger _logger;
    private readonly IProcessFactory _processFactory;
    private readonly IDotNetCommandPathProvider _dotNetCommandPathProvider;

    public ShutDownBuildServer(
        ILogger logger,
        IProcessFactory processFactory,
        IDotNetCommandPathProvider dotNetCommandPathProvider)
    {
        _logger = logger;
        _processFactory = processFactory;
        _dotNetCommandPathProvider = dotNetCommandPathProvider;
    }
        
    public async Task Shutdown()
    {
        try
        {
            using var process = _processFactory.Create(
                new ProcessStartInfo(_dotNetCommandPathProvider.Path, $"build-server shutdown"));
            using var error = process.Output.Concat(process.Error)
                .Subscribe(x => _logger.Information(x));
            await process.Run().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error shutting down build server");
        }
    }
}