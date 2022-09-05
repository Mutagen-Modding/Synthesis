using Noggog.WorkEngine;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.Startup;

public class AddWorkConsumer : IStartupTask
{
    private readonly IWorkConsumer _workConsumer;

    public AddWorkConsumer(IWorkConsumer workConsumer)
    {
        _workConsumer = workConsumer;
    }
    
    public void Start()
    {
        _workConsumer.Start();
    }
}