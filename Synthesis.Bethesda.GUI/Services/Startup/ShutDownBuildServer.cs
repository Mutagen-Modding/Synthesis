﻿using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Noggog.Processes.DI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.GUI.Services.Startup;

public class ShutDownBuildServer
{
    private readonly ILogger _logger;
    private readonly IProcessFactory _processFactory;
    private readonly IDotNetCommandPathProvider _dotNetCommandPathProvider;
    private readonly bool _killWithParent;

    public ShutDownBuildServer(
        ILogger logger,
        IProcessFactory processFactory,
        IDotNetCommandPathProvider dotNetCommandPathProvider)
    {
        _logger = logger;
        _processFactory = processFactory;
        _dotNetCommandPathProvider = dotNetCommandPathProvider;
        _killWithParent = !RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
        
    public async Task Shutdown()
    {
        try
        {
            using var process = _processFactory.Create(
                new ProcessStartInfo(_dotNetCommandPathProvider.Path, $"build-server shutdown"),
                killWithParent: _killWithParent);
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