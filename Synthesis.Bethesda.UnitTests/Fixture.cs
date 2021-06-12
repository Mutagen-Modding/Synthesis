using System;
using System.Reactive.Concurrency;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Kernel;
using Noggog.Reactive;
using NSubstitute;
using Serilog;
using Synthesis.Bethesda.Execution.GitRespository;

namespace Synthesis.Bethesda.UnitTests
{
    public class Fixture : IDisposable
    {
        public ISpecimenBuilder Inject { get; }

        public Fixture()
        {
            var fixture = new AutoFixture.Fixture();
            fixture.Customize(new AutoNSubstituteCustomization());
            fixture.Register<IProvideRepositoryCheckouts>(
                () => new ProvideRepositoryCheckouts(fixture.Create<ILogger>()));
            var scheduler = Substitute.For<ISchedulerProvider>();
            scheduler.TaskPool.Returns(Scheduler.CurrentThread);
            fixture.Register(() => scheduler);
            Inject = fixture;
        }

        public void Dispose()
        {
        }
    }
}