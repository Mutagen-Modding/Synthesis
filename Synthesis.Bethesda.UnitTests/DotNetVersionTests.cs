using FluentAssertions;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            DotNetCommands.GetDotNetVersion(str)
                .Acceptable.Should().Be(shouldSucceed);
        }
    }
}
