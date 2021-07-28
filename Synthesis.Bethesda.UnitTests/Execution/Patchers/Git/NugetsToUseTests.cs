using FluentAssertions;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git
{
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
                .Succeeded.Should().BeFalse();
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
            resp.Succeeded.Should().BeTrue();
            resp.Value.Should().Be(newest);
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
            resp.Succeeded.Should().BeTrue();
            resp.Value.Should().Be(null);
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
            resp.Succeeded.Should().BeTrue();
            resp.Value.Should().Be(manual);
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
                .Succeeded.Should().BeFalse();
        }
    }
}