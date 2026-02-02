using System.Reactive.Concurrency;
using Noggog.Reactive;
using Noggog.WPF;

namespace Synthesis.Bethesda.UnitTests;

public class TestSchedulerProvider : ISchedulerProvider
{
    public IScheduler MainThread => ImmediateScheduler.Instance;
    public IScheduler TaskPool => ImmediateScheduler.Instance;
}
