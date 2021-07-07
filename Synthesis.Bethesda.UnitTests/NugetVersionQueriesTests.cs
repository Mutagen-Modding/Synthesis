using FluentAssertions;
using Synthesis.Bethesda.Execution;
using System;
using System.Collections.Generic;
using System.Text;
using Synthesis.Bethesda.Execution.DotNet;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class NugetVersionQueriesTests
    {
        [Fact]
        public void TypicalNugetParse()
        {
            DotNetCommands.TryParseLibraryLine(
                "   > Mutagen.Bethesda.Synthesis      0.10.7.0    0.10.7   0.10.8.1",
                out var package,
                out var requested,
                out var resolved,
                out var latest)
                .Should().BeTrue();
            package.Should().Be("Mutagen.Bethesda.Synthesis");
            requested.Should().Be("0.10.7.0");
            resolved.Should().Be("0.10.7");
            latest.Should().Be("0.10.8.1");
        }

        [Fact]
        public void DepreciatedNugetParse()
        {
            DotNetCommands.TryParseLibraryLine(
                "   > Mutagen.Bethesda.Synthesis      0.10.7.0    0.10.7 (D)   0.10.8.1",
                out var package,
                out var requested,
                out var resolved,
                out var latest)
                .Should().BeTrue();
            package.Should().Be("Mutagen.Bethesda.Synthesis");
            requested.Should().Be("0.10.7.0");
            resolved.Should().Be("0.10.7");
            latest.Should().Be("0.10.8.1");
        }
    }
}
