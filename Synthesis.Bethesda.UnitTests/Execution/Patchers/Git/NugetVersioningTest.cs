using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git;

public class NugetVersioningTest
{
    [Theory, SynthAutoData]
    public void ReturnIfMatch(
        string version,
        string rhs)
    {
        new NugetVersioningTarget(version, NugetVersioningEnum.Match)
            .ReturnIfMatch(rhs).ShouldBe(rhs);
    }
        
    [Theory]
    [SynthInlineData(NugetVersioningEnum.Latest)]
    [SynthInlineData(NugetVersioningEnum.Manual)]
    public void ReturnIfMatchOther(
        NugetVersioningEnum versioning,
        string version,
        string rhs)
    {
        new NugetVersioningTarget(version, versioning)
            .ReturnIfMatch(rhs).ShouldBe(version);
    }
}