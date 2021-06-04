using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Kernel;
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
            Inject = fixture;
        }

        public void Dispose()
        {
        }
    }
}