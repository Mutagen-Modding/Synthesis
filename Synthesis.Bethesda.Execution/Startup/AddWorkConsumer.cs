using System;
using System.Reactive.Linq;
using Noggog.WorkEngine;
using Serilog;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Startup;

public class AddWorkConsumer : IStartupTask
{
    private readonly IWorkConsumer _workConsumer;
    private readonly ILogger _logger;

    public AddWorkConsumer(
        IWorkConsumer workConsumer,
        ILogger logger)
    {
        _workConsumer = workConsumer;
        _logger = logger;
    }

    public void Start()
    {
        _workConsumer.CurrentCpuCount
            .Select(x => x.DesiredCPUs)
            .DistinctUntilChanged()
            .Skip(1)
            .Subscribe(desired => _logger.Information(
                "Work engine desired worker threads: {DesiredThreads}", desired));
        _workConsumer.Start();
    }
}
