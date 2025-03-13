using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.UnitTests.AutoData;

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
        ret.Mutagen.ShouldBe(pair.Mutagen);
        ret.Synthesis.ShouldBe(pair.Synthesis);
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
        ret.Mutagen.ShouldBe(muta);
        ret.Synthesis.ShouldBe(synth);
    }
}