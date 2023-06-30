using CommandLine;
using FluentAssertions;
using Mutagen.Bethesda;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.UtilityTests;

public class FormatCommandLineTests
{
    [Verb("test-command")]
    class ArgClass
    {
        [Option('s', "Setting")] public string Setting { get; set; } = string.Empty;

        [Option('r', "Release", Required = true)] public GameRelease Release { get; set; }
    }

    [Theory, SynthAutoData]
    public void FormatsBasicCommand(FormatCommandLine sut)
    {
        var format = sut.Format(new ArgClass()
        {
            Setting = "Hello World",
            Release = GameRelease.Fallout4
        });
        format.Should().Be("test-command --Release Fallout4 --Setting \"Hello World\"");
    }

    [Theory, SynthAutoData]
    public void EnumRequiredButDefault(FormatCommandLine sut)
    {
        var format = sut.Format(new RunSynthesisPatcher()
        {
            GameRelease = default
        });
        format.Should().Be("run-patcher --LoadOrderIncludesCreationClub --TargetLanguage English --GameRelease Oblivion");
    }
}