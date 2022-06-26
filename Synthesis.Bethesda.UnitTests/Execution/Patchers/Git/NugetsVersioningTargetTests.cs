using FluentAssertions;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git;

public class NugetsVersioningTargetTests
{
    [Theory, SynthAutoData]
    public void ReturnIfMatch(
        string muta,
        string synth,
        NugetVersionPair pair)
    {
        var ret = new NugetsVersioningTarget(
                new NugetVersioningTarget(muta, NugetVersioningEnum.Match),
                new NugetVersioningTarget(synth, NugetVersioningEnum.Match))
            .ReturnIfMatch(pair);
        ret.Mutagen.Should().Be(pair.Mutagen);
        ret.Synthesis.Should().Be(pair.Synthesis);
    }
        
    [Theory]
    [SynthInlineData(NugetVersioningEnum.Latest)]
    [SynthInlineData(NugetVersioningEnum.Manual)]
    public void ReturnIfMatchOther(
        NugetVersioningEnum versioning,
        string muta,
        string synth,
        NugetVersionPair pair)
    {
        var ret = new NugetsVersioningTarget(
                new NugetVersioningTarget(muta, versioning),
                new NugetVersioningTarget(synth, versioning))
            .ReturnIfMatch(pair);
        ret.Mutagen.Should().Be(muta);
        ret.Synthesis.Should().Be(synth);
    }
}