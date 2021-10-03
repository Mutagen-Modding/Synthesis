using CommandLine;
using FluentAssertions;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.UtilityTests
{
    public class FormatCommandLineTests
    {
        [Verb("test-command")]
        class ArgClass
        {
            [Option('s', "Setting")]
            public string Setting { get; set; } = string.Empty;
        }

        [Theory, SynthAutoData]
        public void FormatsBasicCommand(FormatCommandLine sut)
        {
            sut.Format(new ArgClass()
                {
                    Setting = "Hello World"
                })
                .Should().Be("test-command --Setting \"Hello World\"");
        }
    }
}