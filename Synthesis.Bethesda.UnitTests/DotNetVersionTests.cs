using FluentAssertions;
using Synthesis.Bethesda.Execution.DotNet;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class DotNetVersionTests
    {
        [Theory]
        [InlineData("5.0.1", true)]
        [InlineData("6.0.100-preview.2.21119.3", true)]
        [InlineData("x.5.6", false)]
        public void TestVersionString(string str, bool shouldSucceed)
        {
            var query = new QueryInstalledSdk(null!);
            query.ParseVersionString(str)
                .Acceptable.Should().Be(shouldSucceed);
        }
    }
}
