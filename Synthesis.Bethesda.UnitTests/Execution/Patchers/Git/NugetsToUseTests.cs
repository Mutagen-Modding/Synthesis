using Shouldly;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git;

public class NugetsToUseTests
{
    [Theory, SynthAutoData]
    public void WantsLatestButNotAvailableFails(
        string nickname,
        string manual)
    {
        new NugetsToUse(
                nickname,
                NugetVersioningEnum.Latest,
                manual,
                null)
            .TryGetVersioning()
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void LatestReturnsNewest(
        string nickname,
        string manual,
        string newest)
    {
        var resp = new NugetsToUse(
                nickname,
                NugetVersioningEnum.Latest,
                manual,
                newest)
            .TryGetVersioning();
        resp.Succeeded.ShouldBeTrue();
        resp.Value.ShouldBe(newest);
    }
        
    [Theory, SynthAutoData]
    public void MatchReturnsNull(
        string nickname,
        string manual,
        string newest)
    {
        var resp = new NugetsToUse(
                nickname,
                NugetVersioningEnum.Match,
                manual,
                newest)
            .TryGetVersioning();
        resp.Succeeded.ShouldBeTrue();
        resp.Value.ShouldBe(null);
    }
        
    [Theory, SynthAutoData]
    public void ManualReturnsManual(
        string nickname,
        string manual,
        string newest)
    {
        var resp = new NugetsToUse(
                nickname,
                NugetVersioningEnum.Manual,
                manual,
                newest)
            .TryGetVersioning();
        resp.Succeeded.ShouldBeTrue();
        resp.Value.ShouldBe(manual);
    }
        
    [Theory, SynthAutoData]
    public void EmptyManualFails(
        string nickname,
        string newest)
    {
        new NugetsToUse(
                nickname,
                NugetVersioningEnum.Manual,
                string.Empty,
                newest)
            .TryGetVersioning()
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void EqualsDoesNotConsiderNames(
        string nickname,
        string newest)
    {
        new NugetsToUse(
                nickname,
                NugetVersioningEnum.Manual,
                string.Empty,
                newest)
            .TryGetVersioning()
            .Succeeded.ShouldBeFalse();
    }
}