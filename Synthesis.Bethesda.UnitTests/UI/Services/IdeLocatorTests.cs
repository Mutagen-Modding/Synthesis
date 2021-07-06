using System.IO.Abstractions.TestingHelpers;
using AutoFixture;
using FluentAssertions;
using Serilog;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Services.Main;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.UI.Services
{
    public class IdeLocatorTests: IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public IdeLocatorTests(Fixture fixture)
        {
            _Fixture = fixture;
        }
        
        [Fact]
        public void MissingPathsDoesNotThrow()
        {
            var fileSystem = new MockFileSystem();
            var ideLocator = new IdeLocator(
                _Fixture.Inject.Create<ILogger>(),
                fileSystem);
            ideLocator.VSPath.Should().BeNull();
            ideLocator.RiderPath.Should().BeNull();
        }
    }
}