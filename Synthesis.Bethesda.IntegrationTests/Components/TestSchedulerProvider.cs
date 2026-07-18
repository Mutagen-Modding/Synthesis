using System.Reactive.Concurrency;
using Noggog.Reactive;

namespace Synthesis.Bethesda.IntegrationTests.Components;

public class TestSchedulerProvider : ISchedulerProvider
{
    public IScheduler MainThread { get; } = new EventLoopScheduler();
    public IScheduler TaskPool { get; } = new EventLoopScheduler();
}
